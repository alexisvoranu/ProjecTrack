using Licenta3.Data;
using Licenta3.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard/
        public async Task<IActionResult> Dashboard()
        {
            var taskStatusCounts = await _context.Tasks
                .GroupBy(t => t.State)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var totalTasks = taskStatusCounts.Sum(x => x.Value);

            var projectStatusCounts = await _context.Projects
                .GroupBy(p => p.State)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var totalProjects = projectStatusCounts.Sum(x => x.Value);

            var viewModel = new DashboardViewModel
            {
                TotalTasks = totalTasks,
                TaskStatusCounts = taskStatusCounts,

                TotalProjects = totalProjects,
                ProjectStatusCounts = projectStatusCounts
            };

            return View(viewModel);
        }

        // GET: Members
        public async Task<IActionResult> Members()
        {
            var roleId = "membru";

            var teamMembers = await _context.Users
                .Join(_context.UserRoles,
                      u => u.Id,
                      ur => ur.UserId,
                      (u, ur) => new { User = u, ur.RoleId })
                .Where(x => x.RoleId == roleId)
                .Select(x => x.User)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var memberIds = teamMembers.Select(u => u.Id).ToList();

            var allocatedTasks = await _context.Tasks
                .Where(t => t.UserId != null && memberIds.Contains(t.UserId))
                .Include(t => t.Project)
                .OrderBy(t => t.Project.Name)
                .ThenBy(t => t.Code)
                .ToListAsync();

            var membersWithTasks = new Dictionary<string, List<Models.Task>>();

            foreach (var member in teamMembers)
            {
                var memberName = $"{member.LastName} {member.FirstName}";

                var tasksForMember = allocatedTasks
                    .Where(t => t.UserId == member.Id)
                    .ToList();

                membersWithTasks.Add(memberName, tasksForMember);
            }

            return View(membersWithTasks);
        }

    }
}
