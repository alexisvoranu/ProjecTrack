using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Licenta3.Controllers
{
    public class ResourceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Resource/Index/5
        public async Task<IActionResult> Index(int projectId)
        {
            var projectName = await _context.Projects
                                            .Where(p => p.Id == projectId)
                                            .Select(p => p.Name)
                                            .FirstOrDefaultAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = projectName;

            var resources = await _context.Resources
                                          .Where(r => r.ProjectId == projectId)
                                          .ToListAsync();

            return View(resources);
        }


        // GET: Resource/Create
        public IActionResult Create(int projectId)
        {
            var projectName = _context.Projects
                                      .Where(p => p.Id == projectId)
                                      .Select(p => p.Name)
                                      .FirstOrDefault();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = projectName;

            return View();
        }

        // POST: Resource/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Quantity,MeasurementUnit")] Resource resource, int projectId)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = projectId;
                var projectName = _context.Projects
                                          .Where(p => p.Id == projectId)
                                          .Select(p => p.Name)
                                          .FirstOrDefault();
                ViewBag.ProjectName = projectName;

                return View(resource);
            }

            resource.ProjectId = projectId;

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { projectId = resource.ProjectId });
        }


        // GET: Resource/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            ViewBag.ProjectId = resource.ProjectId;
            return View(resource);
        }

        // POST: Resource/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Quantity,MeasurementUnit,ProjectId")] Resource resource)
        {
            if (id != resource.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = resource.ProjectId;
                return View(resource);
            }

            try
            {
                _context.Update(resource);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResourceExists(resource.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction("Index", new { projectId = resource.ProjectId });
        }

        // GET: Resource/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources
                                         .FirstOrDefaultAsync(r => r.Id == id);
            if (resource == null) return NotFound();

            return View(resource);
        }

        // POST: Resource/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                int projectId = resource.ProjectId;
                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { projectId });
            }
            return NotFound();
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}
