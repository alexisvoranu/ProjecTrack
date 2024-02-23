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
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Code,Name,Dependencies,Duration,MeasurementUnit,ProjectId")] Models.Task task, int id)
		{
			Console.WriteLine("DURATA:");
			Console.WriteLine(task.Duration.ToString());
			task.ProjectId = id;
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
			ViewBag.Id = task.ProjectId;
			ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", task.ProjectId);
			return View(task);
		}

		// POST: Task/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Dependencies,Duration,MeasurementUnit,ProjectId")] Models.Task task, int projectId)
		{
			if (id != task.Id)
			{
				return NotFound();
			}


			try
			{
				_context.Update(task);
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
