using Licenta3.Data;
using Licenta3.Models;
using Licenta3.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
                                       .Include(t => t.Project)
                                       .OrderBy(t => t.Code);

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
                            .OrderBy(t => t.Code)
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

            var newTask = new Licenta3.Models.Task();

            if (id.HasValue)
            {
                newTask.ProjectId = id.Value;

                var projectTasks = await _context.Tasks
                    .Where(t => t.ProjectId == id.Value)
                    .OrderBy(t => t.Code)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Code,
                        Text = $"[{t.Code}] {t.Name}"
                    })
                    .ToListAsync();

                int taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id.Value);

                char nextCodeChar = (char)('A' + taskCount);

                newTask.Code = nextCodeChar.ToString();

                projectTasks.Insert(0, new SelectListItem { Value = "-", Text = "Niciuna (Fără dependențe)" });

                ViewBag.Dependencies = projectTasks;
            }
            else
            {
                ViewBag.Dependencies = new List<SelectListItem>();
                newTask.Code = null;
            }

            return View(newTask);
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

                task.Dependencies = string.Join(",", validDependencies);
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
            task.UserId = null;
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
                selectedCodes = task.Dependencies.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var projectTasks = await _context.Tasks
                .Where(t => t.ProjectId == task.ProjectId)
                .Where(t => t.Id != id)
                .OrderBy(t => t.Code)
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
                .Join(_context.UserRoles,
                      u => u.Id,
                      ur => ur.UserId,
                      (u, ur) => new { User = u, ur.RoleId })
                .Where(x => x.RoleId == roleId)
                .Select(x => x.User)
                .ToListAsync();

            var userList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Neatribuit încă", Selected = string.IsNullOrEmpty(task.UserId) }
            };

            userList.AddRange(usersWithSpecificRole.Select(u =>
                new SelectListItem
                {
                    Value = u.Id,
                    Text = u.LastName + " " + u.FirstName,
                    Selected = u.Id == task.UserId
                }
            ));

            ViewBag.Users = userList;

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;

            var userProjects = _context.Projects
                                       .Where(p => p.UserId == currentUserId)
                                       .ToList();

            ViewData["ProjectId"] = new SelectList(userProjects, "Id", "Name", task.ProjectId, "State");

            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Duration,ProjectId,UserId")] Models.Task task, List<string> selectedDependencies)
        {
            if (id != task.Id)
                return NotFound();

            var existingTask = await _context.Tasks.FindAsync(id);
            if (existingTask == null)
                return NotFound();

            var oldUserId = existingTask.UserId;
            var newUserId = string.IsNullOrEmpty(task.UserId) ? null : task.UserId;

            existingTask.Code = task.Code;
            existingTask.Name = task.Name;
            existingTask.Duration = task.Duration;
            existingTask.ProjectId = task.ProjectId;
            existingTask.UserId = string.IsNullOrEmpty(task.UserId) ? null : task.UserId;

            if (selectedDependencies == null || selectedDependencies.Count == 0 || (selectedDependencies.Count == 1 && selectedDependencies.Contains("-")))
                existingTask.Dependencies = "-";
            else
                existingTask.Dependencies = string.Join(",", selectedDependencies.Where(d => d != "-"));

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tasks.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            if (oldUserId != newUserId)
            {
                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
                var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, "ProjecTrack");
                var project = await _context.Projects
                    .Where(p => p.Id == existingTask.ProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();

                if (oldUserId == null && newUserId != null)
                {
                    var newUser = await _context.Users.FindAsync(newUserId);
                    var to = new EmailAddress(newUser.Email);
                    string subject = $"Ați fost asignat la activitatea – \"{existingTask.Name}\"";

                    string htmlContent = $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8;margin-bottom:10px;'>Status activitate actualizat</h2>
                            <p>Bună ziua,</p>
                            <p>
                                Vă informăm că ați fost asignat la activitatea <strong>{existingTask.Name}</strong>,
                                ce face parte din proiectul <em>{project}</em>.

                            </p>
                            <p>Puteți vizualiza detaliile în aplicația <strong>ProjecTrack</strong>.</p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>";

                    var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                    await client.SendEmailAsync(msg);
                }

                else if (oldUserId != null && newUserId != null && oldUserId != newUserId)
                {
                    var oldUser = await _context.Users.FindAsync(oldUserId);
                    var toOld = new EmailAddress(oldUser.Email);
                    string subjectOld = $"Ați fost deasignat de la activitatea – \"{existingTask.Name}\"";

                    string htmlOld = $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8;margin-bottom:10px;'>Status activitate actualizat</h2>
                            <p>Bună ziua,</p>
                            <p>
                                Vă informăm că ați fost deasignat de la activitatea <strong>{existingTask.Name}</strong>,
                                ce face parte din proiectul <em>{project}</em>.

                            </p>
                            <p>Puteți vizualiza detaliile în aplicația <strong>ProjecTrack</strong>.</p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>";
                    var msgOld = MailHelper.CreateSingleEmail(from, toOld, subjectOld, "", htmlOld);
                    await client.SendEmailAsync(msgOld);

                    var newUser = await _context.Users.FindAsync(newUserId);
                    var toNew = new EmailAddress(newUser.Email);
                    string subjectNew = $"Ați fost asignat la activitatea – \"{existingTask.Name}\"";

                    string htmlNew = $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8;margin-bottom:10px;'>Status activitate actualizat</h2>
                            <p>Bună ziua,</p>
                            <p>
                                Vă informăm că ați fost asignat la activitatea <strong>{existingTask.Name}</strong>,
                                ce face parte din proiectul <em>{project}</em>.

                            </p>
                            <p>Puteți vizualiza detaliile în aplicația <strong>ProjecTrack</strong>.</p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>";
                    var msgNew = MailHelper.CreateSingleEmail(from, toNew, subjectNew, "", htmlNew);
                    await client.SendEmailAsync(msgNew);
                }

                else if (oldUserId != null && newUserId == null)
                {
                    var oldUser = await _context.Users.FindAsync(oldUserId);
                    var to = new EmailAddress(oldUser.Email);
                    string subject = $"Ați fost deasignat de la activitatea – \"{existingTask.Name}\"";
                    string htmlContent = $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8;margin-bottom:10px;'>Status activitate actualizat</h2>
                            <p>Bună ziua,</p>
                            <p>
                                Vă informăm că ați fost deasignat de la activitatea <strong>{existingTask.Name}</strong>,
                                ce face parte din proiectul <em>{project}</em>.

                            </p>
                            <p>Puteți vizualiza detaliile în aplicația <strong>ProjecTrack</strong>.</p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                    await client.SendEmailAsync(msg);
                }
            }

            return RedirectToAction("Index", new { id = existingTask.ProjectId });
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
                return Problem("Entity set 'ApplicationDbContext.Tasks' is null.");
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