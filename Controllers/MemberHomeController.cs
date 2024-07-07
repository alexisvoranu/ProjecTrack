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

            var projectName = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .Select(p => p.Name)
                                            .FirstOrDefaultAsync();

            var startingDate = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .Select(p => p.StartingDate)
                                            .FirstOrDefaultAsync();

            ViewBag.ProjectName = projectName;
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
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingTask = await _context.Tasks.FindAsync(id);
                if (existingTask == null)
                {
                    return NotFound();
                }

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

                string fromMail = "ax.isvoranu@gmail.com";
                string fromPassword = "surzzlxcdadbjhep";

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
                        (combined, user) => new { Task = combined.Task, Project = combined.Project, User = user }
                    )
                    .Select(result => new
                    {
                        TaskId = result.Task.Id,
                        TaskUserId = result.Task.UserId,
                        ProjectName = result.Project.Name,
                        ProjectUserId = result.Project.UserId,
                        UserEmail = result.User.Email,
                        ProjectId = result.Project.Id,
                        ProjectState = result.Project.State
                    })
                    .FirstOrDefaultAsync();

                MailMessage message = new MailMessage();
                message.From = new MailAddress(fromMail);
                message.Subject = string.Format("Status activitate \"{0}\"", existingTask.Name);
                message.To.Add(new MailAddress(taskWithUserInfo.UserEmail));

                message.Body = string.Format("Statusul activității <i>{0}</i> ce face parte din proiectul <i>{1}</i> a fost actualizat!",
                    existingTask.Name, taskWithUserInfo.ProjectName);
                message.IsBodyHtml = true;

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(fromMail, fromPassword),
                    EnableSsl = true,
                };
                smtpClient.Send(message);


                var tasks = await _context.Tasks
                 .Where(t => t.ProjectId == existingTask.ProjectId)
                 .ToListAsync();

                int unfinishedTasks = 0;
                foreach (var task in tasks)
                    if (task.State != "Finalizată" && task.State != "Finalizată cu întârziere")
                        unfinishedTasks++;

                var existingProject = await _context.Projects.FindAsync(existingTask.ProjectId);

                if (existingProject == null)
                {
                    return NotFound();
                }

                if (unfinishedTasks == 0)
                {
                    if (existingProject.State == "În execuție")
                        existingProject.State = "Finalizat";

                    _context.Entry(existingProject).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                if (existingProject.State == "Programat")
                {
                    int StartedTasks = 0;
                    foreach (var task in tasks)
                        if (task.State != "În execuție" || task.State != "Începută cu întârziere")
                            StartedTasks++;

                    if (StartedTasks != 0)
                    {
                        existingProject.State = "În execuție";

                        _context.Entry(existingProject).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            return RedirectToAction("Project");
        }
    }
}
