using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Controllers
{
    public class MemberHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberHomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
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
                                .Include(t => t.Project) // Se alătură tabelului Projects pentru a obține informații despre proiect
                                .Where(t => t.UserId == userId)
                                .ToListAsync();

            var groupedProjects = tasks.GroupBy(t => t.ProjectId.ToString()).ToList(); // Grupare după ID-ul proiectului

            ViewBag.ProjectNames = tasks.Select(t => t.Project.Name).Distinct().ToList(); // Obține și pune numele proiectelor în ViewBag

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

            ViewBag.ProjectName = projectName;
            ViewBag.Id = id;
            return View(await applicationDbContext.ToListAsync());
        }

    }
}
