using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.Build.Framework;
using System.Threading.Tasks;

using SendGrid;
using SendGrid.Helpers.Mail;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta3.Controllers
{
    public class MemberHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberHomeController(ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userid = _userManager.GetUserId(User);

            var tasks = await _context.Tasks
                            .Where(t => t.UserId == userid)
                            .ToListAsync();

            DateTime dataCurenta = DateTime.Today;
            List<Models.Task> allTasks = await _context.Tasks.ToListAsync();

            foreach (var task in allTasks)
            {
                if (DateTime.Compare((DateTime)task.LateStartDate, dataCurenta) < 0 &&
                    (task.State == "Programată" || task.State == "În execuție"))
                {
                    var project = await _context.Projects
                            .Where(p => p.Id == task.ProjectId)
                            .FirstOrDefaultAsync();

                    project.State = "Întârziat";
                    task.State = "Întârziată";

                    _context.Tasks.Update(task);
                    _context.Projects.Update(project);
                    await _context.SaveChangesAsync();
                }
            }

            return View(tasks);
        }

        public async Task<IActionResult> Project()
        {
            var userId = _userManager.GetUserId(User);

            var tasks = await _context.Tasks
                                .Include(t => t.Project)
                                .Where(t => t.UserId == userId)
                                .ToListAsync();

            var groupedProjects = tasks.GroupBy(t => t.ProjectId.ToString()).ToList();

            ViewBag.ProjectNames = tasks.Select(t => t.Project.Name).Distinct().ToList();

            return View(groupedProjects);
        }

        public async Task<IActionResult> Tasks(int? id)
        {
            var applicationDbContext = _context.Tasks
                                       .Where(t => t.ProjectId == id)
                                       .Include(t => t.Project);

            var project = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .FirstOrDefaultAsync();

            var startingDate = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .Select(p => p.StartingDate)
                                            .FirstOrDefaultAsync();

            ViewBag.ProjectName = project.Name;
            ViewBag.UM = project.MeasurementUnit;
            ViewBag.StartingDate = startingDate;
            ViewBag.Id = id;
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Task/Edit/5
        public async Task<IActionResult> Update(int? id)
        {

            var roleId = "membru";

            var usersWithSpecificRole = await _context.Users
                .Join(
                    _context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole }
                )
                .Where(joined => joined.UserRole.RoleId == roleId)
                .Select(joined => joined.User)
                .ToListAsync();

            ViewBag.Users = usersWithSpecificRole;

            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            ViewBag.Id = task.ProjectId;
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", task.ProjectId, "State");
            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, string state)
        {
            if (id == 0)
                return NotFound();

            try
            {
                var existingTask = await _context.Tasks.FindAsync(id);
                if (existingTask == null)
                    return NotFound();

                if (state == "Programată")
                    existingTask.State = "În execuție";
                else if (state == "În execuție")
                    existingTask.State = "Finalizată";
                else if (state == "Întârziată")
                    existingTask.State = "Începută cu întârziere";
                else if (state == "Începută cu întârziere")
                    existingTask.State = "Finalizată cu întârziere";

                _context.Entry(existingTask).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                var taskWithUserInfo = await _context.Tasks
                    .Where(task => task.Id == id)
                    .Join(
                        _context.Projects,
                        task => task.ProjectId,
                        project => project.Id,
                        (task, project) => new { Task = task, Project = project }
                    )
                    .Join(
                        _context.Users,
                        combined => combined.Project.UserId,
                        user => user.Id,
                        (combined, user) => new { combined.Task, combined.Project, User = user }
                    )
                    .Select(result => new
                    {
                        result.Task.Name,
                        ProjectName = result.Project.Name,
                        result.User.Email
                    })
                    .FirstOrDefaultAsync();

                if (taskWithUserInfo != null)
                {
                    var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
                    var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
                    var client = new SendGridClient(apiKey);
                    var from = new EmailAddress(fromEmail, "ProjecTrack");
                    var to = new EmailAddress(taskWithUserInfo.Email);

                    string subject = $"Actualizare status activitate – \"{taskWithUserInfo.Name}\"";

                    string htmlContent = $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8;margin-bottom:10px;'>Status activitate actualizat</h2>
                            <p>Bună ziua,</p>
                            <p>
                                Vă informăm că statusul activității <strong>{taskWithUserInfo.Name}</strong>,
                                care face parte din proiectul <em>{taskWithUserInfo.ProjectName}</em>,
                                a fost actualizat cu succes. ✅
                            </p>
                            <p>
                                Puteți vizualiza modificarea completă în aplicația <strong>ProjecTrack</strong>.
                            </p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>";

                    var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                    var response = await client.SendEmailAsync(msg);
                }

                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == existingTask.ProjectId)
                    .ToListAsync();

                int unfinishedTasks = tasks.Count(t => t.State != "Finalizată" && t.State != "Finalizată cu întârziere");

                var existingProject = await _context.Projects.FindAsync(existingTask.ProjectId);
                if (existingProject == null)
                    return NotFound();

                if (unfinishedTasks == 0 && existingProject.State == "În execuție")
                {
                    existingProject.State = "Finalizat";
                    _context.Entry(existingProject).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                if (existingProject.State == "Programat")
                {
                    int startedTasks = tasks.Count(t => t.State == "În execuție" || t.State == "Începută cu întârziere");
                    if (startedTasks > 0)
                    {
                        existingProject.State = "În execuție";
                        _context.Entry(existingProject).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la Update(): {ex.Message}");
                throw;
            }

            return RedirectToAction("Project");
        }

    }
}
