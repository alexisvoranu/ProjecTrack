using Licenta3.Data;
using Licenta3.Models;
using Licenta3.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Controllers
{
    public class TaskResourceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskResourceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TaskResource/Manage/{taskId}
        public async Task<IActionResult> Manage(int taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound();

            ViewBag.projectId = task.ProjectId;

            var resources = await _context.Resources
                .Where(r => r.ProjectId == task.ProjectId)
                .ToListAsync();

            var usedPerResourceForThisTask = await _context.TaskResources
                .Where(tr => tr.TaskId == taskId)
                .GroupBy(tr => tr.ResourceId)
                .Select(g => new { ResourceId = g.Key, Used = g.Sum(tr => tr.QuantityUsed) })
                .ToDictionaryAsync(x => x.ResourceId, x => x.Used);

            var availableResources = resources
                .Select(r => new
                {
                    Resource = r,
                    Remaining = r.Quantity - (usedPerResourceForThisTask.ContainsKey(r.Id) ? usedPerResourceForThisTask[r.Id] : 0)
                })
                .Where(r => r.Remaining > 0)
                .Select(r => new SelectListItem
                {
                    Value = r.Resource.Id.ToString(),
                    Text = $"{r.Resource.Name} ({r.Remaining} {r.Resource.MeasurementUnit} disponibile)"
                })
                .ToList();

            var assignedResources = await _context.TaskResources
                .Where(tr => tr.TaskId == taskId)
                .Include(tr => tr.Resource)
                .ToListAsync();

            var viewModel = new TaskResourceManageViewModel
            {
                Task = task,
                AssignedResources = assignedResources,
                AvailableResources = availableResources
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int taskId, int resourceId, decimal quantityUsed)
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null || resource == null)
                return NotFound();

            var alreadyUsed = await _context.TaskResources
                .Where(tr => tr.TaskId == taskId && tr.ResourceId == resourceId)
                .SumAsync(tr => (decimal?)tr.QuantityUsed) ?? 0;

            var maxAvailable = resource.Quantity - alreadyUsed;

            if (quantityUsed <= 0)
            {
                TempData["Error"] = "Cantitatea trebuie să fie mai mare decât zero!";
                return RedirectToAction("Manage", new { taskId });
            }

            if (quantityUsed > maxAvailable)
            {
                TempData["Error"] = $"Cantitatea introdusă depășește cantitatea maximă disponibilă pentru această activitate! Max: {maxAvailable}";
                return RedirectToAction("Manage", new { taskId });
            }

            var existingRelation = await _context.TaskResources
                .FirstOrDefaultAsync(tr => tr.TaskId == taskId && tr.ResourceId == resourceId);

            if (existingRelation != null)
            {
                existingRelation.QuantityUsed += quantityUsed;
            }
            else
            {
                var taskResource = new TaskResource
                {
                    TaskId = taskId,
                    ResourceId = resourceId,
                    QuantityUsed = quantityUsed
                };
                _context.TaskResources.Add(taskResource);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Manage", new { taskId });
        }

        // POST: TaskResource/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int taskId)
        {
            var tr = await _context.TaskResources.FindAsync(id);
            if (tr != null)
            {
                _context.TaskResources.Remove(tr);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Manage", new { taskId });
        }
    }
}
