using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

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

                // Modifică doar starea
                if (state == "Programată")
                    existingTask.State = "Începută";
                else if (state == "Începută" || state == "Întârziată")
                    existingTask.State = "Finalizată";

                _context.Entry(existingTask).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            return RedirectToAction("Project");
        }
    }
}
