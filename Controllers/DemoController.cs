using Microsoft.AspNetCore.Mvc;
using Licenta3.Models;
using Licenta3.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Controllers
{
    public class DemoController : Controller
    {
        public class DemoTask : Licenta3.Models.Task
        {
            [NotMapped] public DateTime EarlyStartDate { get; set; }
            [NotMapped] public DateTime EarlyFinishDate { get; set; }
            [NotMapped] public DateTime LateFinishDate { get; set; }
            [NotMapped] public decimal Slack { get; set; }
            [NotMapped] public bool IsCritical { get; set; }
            [NotMapped] public decimal ScheduledFinish { get; set; }
        }

        private class DemoDataContext
        {
            public Project Project { get; set; }
            public List<DemoTask> Tasks { get; set; }
            public List<Resource> Resources { get; set; }
            public List<TaskResource> Allocations { get; set; }

            public DemoDataContext()
            {
                Project = new Project
                {
                    Id = 2,
                    Name = "Sistem Bancar Core-Banking",
                    MeasurementUnit = "săptămâni",
                    StartingDate = DateTime.Now.Date,
                    State = "În execuție"
                };

                Resources = new List<Resource>
                {
                    new Resource { Id = 10, Name = "Tiglă Ceramică", Quantity = 2000, MeasurementUnit = "buc", ProjectId = 2 },
                    new Resource { Id = 11, Name = "Senior Lead Dev", Quantity = 160, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 12, Name = "Junior Dev", Quantity = 600, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 13, Name = "Server Staging", Quantity = 2, MeasurementUnit = "buc", ProjectId = 2 },
                    new Resource { Id = 14, Name = "Licențe Cloud", Quantity = 20, MeasurementUnit = "buc", ProjectId = 2 },
                    new Resource { Id = 15, Name = "Consultant Securitate", Quantity = 40, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 16, Name = "DevOps Engineer", Quantity = 80, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 17, Name = "Project Manager", Quantity = 160, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 18, Name = "QA Tester", Quantity = 200, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 19, Name = "DBA (Database Admin)", Quantity = 80, MeasurementUnit = "ore", ProjectId = 2 },
                    new Resource { Id = 20, Name = "MacBook Pro", Quantity = 10, MeasurementUnit = "buc", ProjectId = 2 }
                };

                var start = Project.StartingDate;

                Tasks = new List<DemoTask>
                {
                    new DemoTask { Id = 12, Code = "T1", Name = "Arhitectură Sistem", Duration = "2", Dependencies = "-", State = "Finalizată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start, EarlyFinishDate = start.AddDays(14), LateStartDate = start, LateFinishDate = start.AddDays(14), IsCritical = true, Slack = 0, ScheduledFinish = 2 },

                    new DemoTask { Id = 13, Code = "T2", Name = "Dezvoltare Backend API", Duration = "10", Dependencies = "T1", State = "În execuție", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(14), EarlyFinishDate = start.AddDays(84), LateStartDate = start.AddDays(14), LateFinishDate = start.AddDays(84), IsCritical = true, Slack = 0, ScheduledFinish = 12 },

                    new DemoTask { Id = 14, Code = "T3", Name = "Dezvoltare Frontend Web", Duration = "8", Dependencies = "T1", State = "În execuție", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(14), EarlyFinishDate = start.AddDays(70), LateStartDate = start.AddDays(28), LateFinishDate = start.AddDays(84), IsCritical = false, Slack = 2, ScheduledFinish = 10 },

                    new DemoTask { Id = 15, Code = "T4", Name = "Dezvoltare iOS/Android", Duration = "9", Dependencies = "T1", State = "În execuție", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(14), EarlyFinishDate = start.AddDays(77), LateStartDate = start.AddDays(21), LateFinishDate = start.AddDays(84), IsCritical = false, Slack = 1, ScheduledFinish = 11 },

                    new DemoTask { Id = 16, Code = "T5", Name = "Integrare Module", Duration = "5", Dependencies = "T2,T3,T4", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(84), EarlyFinishDate = start.AddDays(119), LateStartDate = start.AddDays(84), LateFinishDate = start.AddDays(119), IsCritical = true, Slack = 0, ScheduledFinish = 17 },

                    new DemoTask { Id = 17, Code = "T6", Name = "Audit Securitate", Duration = "3", Dependencies = "T5", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(119), EarlyFinishDate = start.AddDays(140), LateStartDate = start.AddDays(133), LateFinishDate = start.AddDays(154), IsCritical = false, Slack = 2, ScheduledFinish = 20 },

                    new DemoTask { Id = 18, Code = "T7", Name = "Optimizare Indecși DB", Duration = "2", Dependencies = "T5", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(119), EarlyFinishDate = start.AddDays(133), LateStartDate = start.AddDays(119), LateFinishDate = start.AddDays(133), IsCritical = true, Slack = 0, ScheduledFinish = 19 },

                    new DemoTask { Id = 20, Code = "T9", Name = "Teste de Performanță", Duration = "3", Dependencies = "T7,T5", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(133), EarlyFinishDate = start.AddDays(154), LateStartDate = start.AddDays(133), LateFinishDate = start.AddDays(154), IsCritical = true, Slack = 0, ScheduledFinish = 22 },

                    new DemoTask { Id = 19, Code = "T8", Name = "QA Manual & Bugfix", Duration = "5", Dependencies = "T6", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(140), EarlyFinishDate = start.AddDays(175), LateStartDate = start.AddDays(154), LateFinishDate = start.AddDays(189), IsCritical = false, Slack = 2, ScheduledFinish = 25 },

                    new DemoTask { Id = 21, Code = "T10", Name = "UAT (Acceptanță)", Duration = "2", Dependencies = "T8,T9", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(175), EarlyFinishDate = start.AddDays(189), LateStartDate = start.AddDays(189), LateFinishDate = start.AddDays(203), IsCritical = true, Slack = 0, ScheduledFinish = 27 },

                    new DemoTask { Id = 22, Code = "T11", Name = "Go Live (Deploy)", Duration = "1", Dependencies = "T10", State = "Programată", ProjectId = 2, Project = Project,
                        EarlyStartDate = start.AddDays(189), EarlyFinishDate = start.AddDays(196), LateStartDate = start.AddDays(203), LateFinishDate = start.AddDays(210), IsCritical = true, Slack = 0, ScheduledFinish = 28 }
                };

                Allocations = new List<TaskResource>
                {
                    new TaskResource { Id=22, TaskId = 12, ResourceId = 17, QuantityUsed = 20 },

                    new TaskResource { Id=23, TaskId = 13, ResourceId = 11, QuantityUsed = 80 },
                    new TaskResource { Id=24, TaskId = 13, ResourceId = 12, QuantityUsed = 100 },

                    new TaskResource { Id=25, TaskId = 14, ResourceId = 11, QuantityUsed = 80 },
                    new TaskResource { Id=26, TaskId = 14, ResourceId = 12, QuantityUsed = 100 },

                    new TaskResource { Id=27, TaskId = 15, ResourceId = 11, QuantityUsed = 80 },
                    new TaskResource { Id=28, TaskId = 15, ResourceId = 12, QuantityUsed = 100 },

                    new TaskResource { Id=29, TaskId = 16, ResourceId = 11, QuantityUsed = 40 },
                    new TaskResource { Id=30, TaskId = 16, ResourceId = 16, QuantityUsed = 40 },
                    new TaskResource { Id=31, TaskId = 17, ResourceId = 15, QuantityUsed = 40 },
                    new TaskResource { Id=32, TaskId = 18, ResourceId = 19, QuantityUsed = 40 },
                    new TaskResource { Id=33, TaskId = 19, ResourceId = 18, QuantityUsed = 100 },
                    new TaskResource { Id=34, TaskId = 20, ResourceId = 18, QuantityUsed = 50 },
                    new TaskResource { Id=35, TaskId = 21, ResourceId = 17, QuantityUsed = 20 },
                    new TaskResource { Id=36, TaskId = 22, ResourceId = 16, QuantityUsed = 10 }
                };
            }
        }

        public IActionResult Index()
        {
            var data = new DemoDataContext();

            ViewBag.ProjectName = data.Project.Name;
            ViewBag.StartingDate = data.Project.StartingDate.ToString("dd/MM/yyyy");
            ViewBag.Id = data.Project.Id;
            ViewBag.IsDemo = true;
            ViewBag.Um = data.Project.MeasurementUnit;

            ViewBag.UserNames = data.Tasks.Select(t => "Demo User").ToList();

            var tasksForView = data.Tasks.Cast<Licenta3.Models.Task>().ToList();

            return View("~/Views/Task/Index.cshtml", tasksForView);
        }

        // GET: Demo/Details/12
        public IActionResult Details(int id)
        {
            var data = new DemoDataContext();
            var task = data.Tasks.FirstOrDefault(t => t.Id == id);

            if (task == null) return NotFound();

            var deps = new List<(string, string)>();
            if (task.Dependencies != null && task.Dependencies != "-")
            {
                foreach (var depCode in task.Dependencies.Split(','))
                {
                    var depName = data.Tasks.FirstOrDefault(t => t.Code == depCode)?.Name ?? "Necunoscut";
                    deps.Add((depCode, depName));
                }
            }
            ViewBag.Dependencies = deps;
            ViewBag.Um = data.Project.MeasurementUnit;

            var resources = from alloc in data.Allocations
                            join res in data.Resources on alloc.ResourceId equals res.Id
                            where alloc.TaskId == id
                            select new TaskResourceDisplayViewModel
                            {
                                ResourceName = res.Name,
                                QuantityUsed = alloc.QuantityUsed,
                                MeasurementUnit = res.MeasurementUnit
                            };

            ViewBag.Resources = resources.ToList();
            ViewBag.IsDemo = true;

            return View("~/Views/Task/Details.cshtml", task);
        }

        public IActionResult ManageResources(int id)
        {
            var data = new DemoDataContext();

            var task = data.Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();

            var assignedResources = data.Allocations
                .Where(a => a.TaskId == id)
                .Join(data.Resources,
                      alloc => alloc.ResourceId,
                      res => res.Id,
                      (alloc, res) => new TaskResource
                      {
                          Id = alloc.Id,
                          TaskId = alloc.TaskId,
                          ResourceId = alloc.ResourceId,
                          QuantityUsed = alloc.QuantityUsed,
                          Resource = res
                      })
                .ToList();

            var availableResources = data.Resources
                .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.MeasurementUnit})"
                })
                .ToList();

            var model = new Licenta3.Models.ViewModels.TaskResourceManageViewModel
            {
                Task = task,
                AssignedResources = assignedResources,
                AvailableResources = availableResources
            };

            ViewBag.projectId = data.Project.Id;
            ViewBag.IsDemo = true;

            return View("~/Views/TaskResource/Manage.cshtml", model);
        }
    }
}