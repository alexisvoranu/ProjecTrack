using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace Licenta3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> IndexAsync()
        {
            DateTime dataCurenta = DateTime.Today;
            List<Models.Task> tasks = await _context.Tasks.ToListAsync();

            foreach (var task in tasks)
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

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
