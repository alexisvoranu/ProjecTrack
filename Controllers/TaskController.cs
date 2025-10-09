using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Licenta3.Data;
using Licenta3.Models;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.AspNetCore.Identity;
using Licenta3.Models.ViewModels;

namespace Licenta3.Controllers
{
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Task
        public async Task<IActionResult> Index(int? id)
        {
            var applicationDbContext = _context.Tasks
                                       .Where(t => t.ProjectId == id)
                                       .Include(t => t.Project);

            string um = await _context.Projects
                                    .Where(t => t.Id == id)
                                    .Select(t => t.MeasurementUnit)
                                    .FirstOrDefaultAsync();

            var projectName = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .Select(p => p.Name)
                                            .FirstOrDefaultAsync();

            var userId = await _context.Projects
                                    .Where(p => p.Id == id)
                                    .Select(p => p.UserId)
                                    .FirstOrDefaultAsync();

            var tasksWithNames = await _context.Tasks
                            .Where(t => t.ProjectId == id)
                            .Include(t => t.Project)
                            .Select(t => new
                            {
                                Task = t,
                                UserName = t.UserId != userId ?
                                            _context.Users
                                                    .Where(u => u.Id == t.UserId)
                                                    .Select(u => u.LastName + " " + u.FirstName)
                                                    .FirstOrDefault() :
                                            "Neatribuit încă"
                            })
                            .ToListAsync();

            var userNames = tasksWithNames.Select(t => t.UserName).ToList();

            ViewBag.UserNames = userNames;
            ViewBag.Um = um;

            ViewBag.ProjectName = projectName;
            ViewBag.Id = id;

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Tasks == null)
                return NotFound();

            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null)
                return NotFound();

            string um = await _context.Projects
                .Where(p => p.Id == task.ProjectId)
                .Select(p => p.MeasurementUnit)
                .FirstOrDefaultAsync();
            ViewBag.Um = um;

            List<ValueTuple<string, string>> dependenciesList = new List<ValueTuple<string, string>>();

            if (!string.IsNullOrEmpty(task.Dependencies) && task.Dependencies != "-")
            {
                var dependencyCodes = task.Dependencies
                    .Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var dependenciesData = await _context.Tasks
                    .Where(t => t.ProjectId == task.ProjectId) 
                    .Where(t => dependencyCodes.Contains(t.Code))
                    .Select(t => new { t.Code, t.Name })
                    .AsNoTracking()
                    .ToListAsync();

                dependenciesList = dependenciesData
                    .Select(x => (x.Code, x.Name))
                    .Distinct()
                    .ToList();
            }

            ViewBag.Dependencies = dependenciesList;

            var resources = await _context.TaskResources
                .Where(r => r.TaskId == task.Id)
                .Include(r => r.Resource)
                .Select(r => new TaskResourceDisplayViewModel
                {
                    ResourceName = r.Resource.Name,
                    QuantityUsed = r.QuantityUsed,
                    MeasurementUnit = r.Resource.MeasurementUnit
                })
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Resources = resources;

            return View(task);
        }

        // GET: Task/Create
        public async Task<IActionResult> Create(int? id)
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            ViewBag.ProjectId = id;

            if (id.HasValue)
            {
                var projectTasks = await _context.Tasks
                    .Where(t => t.ProjectId == id.Value)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Code,
                        Text = $"[{t.Code}] {t.Name}"
                    })
                    .ToListAsync();

                projectTasks.Insert(0, new SelectListItem { Value = "-", Text = "Niciuna (Fără dependențe)" });

                ViewBag.Dependencies = projectTasks;
            }
            else
            {
                ViewBag.Dependencies = new List<SelectListItem>();
            }

            return View();
        }

        // POST: Task/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Duration,ProjectId,State")] Models.Task task, int id, List<string> selectedDependencies)
        {
            if (selectedDependencies == null || selectedDependencies.Count == 0 || (selectedDependencies.Count == 1 && selectedDependencies.Contains("-")))
            {
                task.Dependencies = "-";
            }
            else
            {
                var validDependencies = selectedDependencies.Where(d => d != "-").ToList();

                task.Dependencies = string.Join(", ", validDependencies);
            }

            var userId = await _context.Projects
                                .Where(p => p.Id == id)
                                .Select(p => p.UserId)
                                .FirstOrDefaultAsync();

            var project = await _context.Projects
                                .Where(p => p.Id == id)
                                .FirstOrDefaultAsync();

            project.State = "În execuție";

            if (task.LateStartDate == null)
                task.LateStartDate = DateTime.Now.AddYears(1);

            task.State = "Programată";
            task.UserId = userId;
            task.ProjectId = id;
            ViewBag.ProjectId = task.ProjectId;

            _context.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = id });
        }

        // GET: Task/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            List<string> selectedCodes = new List<string>();
            if (!string.IsNullOrEmpty(task.Dependencies) && task.Dependencies != "-")
            {
                selectedCodes = task.Dependencies.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var projectTasks = await _context.Tasks
                .Where(t => t.ProjectId == task.ProjectId)
                .Where(t => t.Id != id)
                .Select(t => new SelectListItem
                {
                    Value = t.Code,
                    Text = $"[{t.Code}] {t.Name}",
                    Selected = selectedCodes.Contains(t.Code)
                })
                .ToListAsync();

            projectTasks.Insert(0, new SelectListItem
            {
                Value = "-",
                Text = "Niciuna (Fără dependențe)",
                Selected = task.Dependencies == "-" || string.IsNullOrEmpty(task.Dependencies)
            });

            ViewBag.Dependencies = projectTasks;

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
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", task.ProjectId, "State");

            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Duration,ProjectId,UserId")] Models.Task task, int projectId, List<string> selectedDependencies)
        {
            if (id != task.Id)
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

                if (selectedDependencies == null || selectedDependencies.Count == 0 || (selectedDependencies.Count == 1 && selectedDependencies.Contains("-")))
                {
                    existingTask.Dependencies = "-";
                }
                else
                {
                    var validDependencies = selectedDependencies.Where(d => d != "-").ToList();
                    existingTask.Dependencies = string.Join(", ", validDependencies);
                }

                existingTask.Code = task.Code;
                existingTask.Name = task.Name;
                existingTask.Duration = task.Duration;
                existingTask.UserId = task.UserId;

                _context.Entry(existingTask).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(task.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Index", new { id = projectId });
        }

        // GET: Task/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            string um = await _context.Projects
            .Where(p => p.Id == task.ProjectId)
            .Select(p => p.MeasurementUnit)
            .FirstOrDefaultAsync();

            ViewBag.Um = um;

            return View(task);
        }

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tasks == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Tasks'  is null.");
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
            }

            var projectId = task.ProjectId;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { id = projectId });
        }

        private bool TaskExists(int id)
        {
            return (_context.Tasks?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
