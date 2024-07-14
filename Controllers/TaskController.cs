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

            var projectName = await _context.Projects
                                            .Where(p => p.Id == id)
                                            .Select(p => p.Name)
                                            .FirstOrDefaultAsync();

            //Id-ul utilizatorului care are proiectul cu Id-ul specificat
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

            ViewBag.ProjectName = projectName;
            ViewBag.Id = id;

            return View(await applicationDbContext.ToListAsync());
        }


        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
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

            return View(task);
        }

        // GET: Task/Create
        public IActionResult Create(int? id)
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            ViewBag.Id = id;
            return View();
        }

        // POST: Task/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Dependencies,Duration,MeasurementUnit,ProjectId,State")] Models.Task task, int id)
        {
            //Id-ul utilizatorului care are proiectul cu Id-ul specificat
            var userId = await _context.Projects
                                    .Where(p => p.Id == id)
                                    .Select(p => p.UserId)
                                    .FirstOrDefaultAsync();

            var project = await _context.Projects
                                    .Where(p => p.Id == id)
                                    .FirstOrDefaultAsync();

            project.State = "În execuție";

            if (task.Dependencies == null || task.Dependencies == "")
                task.Dependencies = "-";

            if (task.LateStartDate == null)
                task.LateStartDate = DateTime.Now.AddYears(1);

            task.State = "Programată";
            task.UserId = userId;
            task.ProjectId = id;
            _context.Add(task);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { id = id });
        }

        // GET: Task/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Dependencies,Duration,MeasurementUnit,ProjectId,UserId")] Models.Task task, int projectId)
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

                existingTask.Code = task.Code;
                existingTask.Name = task.Name;
                if (task.Dependencies == null || task.Dependencies == "")
                    existingTask.Dependencies = "-";
                else
                    existingTask.Dependencies = task.Dependencies;
                existingTask.Duration = task.Duration;
                existingTask.MeasurementUnit = task.MeasurementUnit;
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
