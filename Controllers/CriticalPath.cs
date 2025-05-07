using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Licenta3.Controllers
{
    public class CriticalPath : Controller
    {
        private readonly ApplicationDbContext _context;
        private List<Activity> Activities = new List<Activity>();

        public CriticalPath(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int id)
        {
            int projectId = await _context.Tasks
                                        .Where(t => t.Id == id)
                                        .Select(t => t.ProjectId)
                                        .FirstOrDefaultAsync();

            await CalculateCriticalPath(projectId);

            var activity = Activities.Find(x => x.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }


        public async Task<IActionResult> CalculateCriticalPath(int? id)
        {

            List<int> ES = new List<int>();
            List<int> LF = new List<int>();
            List<int> Slack = new List<int>();
            List<Activity> checkedActivities = new List<Activity>();

            string um = "";

            decimal maxLF = 0;
            int STOPposition = 0;

            List<Models.Task> tasks = await _context.Tasks
                                         .Where(t => t.ProjectId == id)
                                         .ToListAsync();

            DateTime startingDate = await _context.Projects
                                    .Where(t => t.Id == id)
                                    .Select(t => t.StartingDate)
                                    .FirstOrDefaultAsync();

            ViewBag.StartingDate = startingDate;

            string projectName = await _context.Projects
                                    .Where(t => t.Id == id)
                                    .Select(t => t.Name)
                                    .FirstOrDefaultAsync();

            ViewBag.ProjectName = projectName;

            foreach (var task in tasks)
            {
                Activity activitate = new Activity(task.Id, task.Code, task.Name, task.Dependencies, decimal.Parse(task.Duration), task.MeasurementUnit, task.State);
                Activities.Add(activitate);
                um = task.MeasurementUnit.ToString();
            }

            ViewBag.Um = um;

            foreach (var activity in Activities)
            {
                if (activity.Dependencies == "-")
                {
                    activity.EarlyStart = 0; // Activitatea de pornire
                    activity.EarlyStartDate = startingDate;
                    activity.EarlyFinish = activity.Duration;
                    activity.EarlyFinishDate = startingDate.AddDays((double)activity.Duration);
                    activity.Position = 0;
                    checkedActivities.Add(activity);
                }
            }

            //sortez activitatile
            while (checkedActivities.Count != Activities.Count)
                foreach (var activity in Activities)
                {

                    if (activity.Dependencies != "-" && !(checkedActivities.Contains(activity)))
                    {
                        var dependencyIds = activity.Dependencies.Split(',');
                        bool ok = true;

                        foreach (var idStr in dependencyIds)
                        {
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                                if (!checkedActivities.Contains(dependentActivity))
                                {
                                    ok = false;
                                    break;
                                }
                            }
                        }

                        if (ok == true)
                        {
                            checkedActivities.Add((Activity)activity);
                        }
                    }
                }

            Activities = checkedActivities;

            // Pasul 2: Calcul Early Start (ES) pentru fiecare activitate
            foreach (var activity in Activities)
            {
                if (activity.Dependencies != "-")
                {
                    var dependencyIds = activity.Dependencies.Split(',');

                    decimal maxDependencyES = 0;

                    foreach (var idStr in dependencyIds)
                    {

                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (dependentActivity != null)
                            {
                                maxDependencyES = Math.Max(maxDependencyES, dependentActivity.EarlyFinish);
                            }

                        }
                    }

                    activity.EarlyStart = maxDependencyES;
                    activity.EarlyFinish = maxDependencyES + activity.Duration;
                    activity.EarlyStartDate = startingDate.AddDays((double)maxDependencyES);
                    activity.EarlyFinishDate = startingDate.AddDays((double)maxDependencyES + (double)activity.Duration);

                    //calculam EF pentru nodul fictiv
                    if (activity.EarlyFinish > maxLF)
                        maxLF = activity.EarlyFinish;

                    int maxPosition = 0;

                    foreach (var idStr in dependencyIds)
                    //parcurgem fiecare activitate din vector pentru a afla cea mai mare pozitie
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (dependentActivity != null)
                            {
                                maxPosition = Math.Max(maxPosition, dependentActivity.Position);
                            }
                        }
                    }

                    int numar = maxPosition + 1;
                    activity.Position = numar;
                    if (numar > STOPposition)
                        STOPposition = numar;
                }

            }

            ViewBag.MaxPosition = STOPposition;

            //calculam Inclusion
            foreach (var activityP in Activities)
            {
                activityP.Inclusion = string.Join(",", Activities
                    .Where(activityL => activityL != activityP && activityL.Dependencies.Contains(activityP.Code))
                    .Select(activityL => activityL.Code));
            }

            foreach (var activity in Activities)
            {
                if (string.IsNullOrEmpty(activity.Inclusion))
                    activity.Inclusion = "-";
            }


            // Pasul 3: Calcul Late Finish (LF) pentru fiecare activitate
            // Parcurg activitatile in ordine inversa

            var LFT = Activities[Activities.Count - 1].EarlyFinish;


            for (int i = Activities.Count - 1; i >= 0; i--)
            {
                var activity = Activities[i];


                if (string.IsNullOrEmpty(activity.Inclusion) || activity.Inclusion == "-")
                {
                    // Activitatea finala sau fara dependente
                    activity.LateFinish = maxLF;
                    activity.LateFinishDate = startingDate.AddDays((double)maxLF);
                    activity.LateStart = activity.LateFinish - activity.Duration;
                    activity.LateStartDate = startingDate.AddDays((double)activity.LateFinish
                        - (double)activity.Duration);

                    var task = await _context.Tasks
                        .Where(t => t.Id == activity.Id)
                        .FirstOrDefaultAsync();

                    if (task != null)
                    {
                        task.LateStartDate = activity.LateStartDate;
                        _context.Tasks.Update(task);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Calculez LF bazat pe dependente

                    var inclusionIds = activity.Inclusion.Split(',');
                    decimal mininclusionLF = decimal.MaxValue;

                    foreach (var idStr in inclusionIds)
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var inclusionActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (inclusionActivity != null)
                            {
                                mininclusionLF = Math.Min(mininclusionLF, inclusionActivity.LateStart);
                            }
                        }
                    }

                    activity.LateFinish = mininclusionLF;
                    activity.LateFinishDate = startingDate.AddDays((double)mininclusionLF);
                    activity.LateStart = activity.LateFinish - activity.Duration;
                    activity.LateStartDate = startingDate.AddDays((double)mininclusionLF
                        - (double)activity.Duration);

                    var task = await _context.Tasks
                        .Where(t => t.Id == activity.Id)
                        .FirstOrDefaultAsync();

                    if (task != null)
                    {
                        task.LateStartDate = activity.LateStartDate;
                        _context.Tasks.Update(task);
                        await _context.SaveChangesAsync();
                    }
                }
            }


            // Pasul 4: Calcul Slack si identificarea activitatilor critice
            foreach (var activity in Activities)
            {
                activity.Slack = activity.LateStart - activity.EarlyStart;
                if (activity.Slack == 0)
                {
                    activity.IsCritical = true;
                }
            }

            string finalActivities = "";

            foreach (var activity in Activities)
            {
                if (activity.Inclusion == "-")
                    finalActivities += activity.Code + ",";
            }
            finalActivities = finalActivities.Remove(finalActivities.Length - 1);

            Activity FinalActivity = new Activity(0, "STOP", "STOP", finalActivities,
                0, "", "", maxLF, maxLF, maxLF, maxLF, 0, true, "-", STOPposition + 1);
            FinalActivity.EarlyStartDate = startingDate.AddDays((double)maxLF);
            FinalActivity.EarlyFinishDate = startingDate.AddDays((double)maxLF);
            FinalActivity.LateStartDate = startingDate.AddDays((double)maxLF);
            FinalActivity.LateFinishDate = startingDate.AddDays((double)maxLF);
            Activities.Add(FinalActivity);

            DateTime finishingDate;

            switch (um)
            {
                case "minute":
                    finishingDate = startingDate.AddMinutes((double)maxLF);
                    break;
                case "ore":
                    finishingDate = startingDate.AddHours((double)maxLF);
                    break;
                case "zile":
                    finishingDate = startingDate.AddDays((double)maxLF);
                    break;
                case "luni":
                    finishingDate = startingDate.AddMonths((int)maxLF);
                    break;
                case "ani":
                    finishingDate = startingDate.AddYears((int)maxLF);
                    break;
                default:
                    finishingDate = startingDate;
                    break;
            }

            ViewBag.FinishingDate = finishingDate;

            ViewBag.Id = id;

            List<List<Activity>> criticalPaths = new List<List<Activity>>();
            void FindCriticalPaths(Activity currentActivity, List<Activity> currentPath, List<List<Activity>> allPaths)
            {
                currentPath.Add(currentActivity);

                if (currentActivity.Dependencies == "-")
                {
                    allPaths.Add(new List<Activity>(currentPath));
                }
                else
                {
                    var dependencyIds = currentActivity.Dependencies.Split(',');

                    foreach (var idStr in dependencyIds)
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var nextActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (nextActivity != null && nextActivity.IsCritical && !currentPath.Contains(nextActivity))
                            {
                                FindCriticalPaths(nextActivity, currentPath, allPaths);
                            }
                        }
                    }
                }

                currentPath.Remove(currentActivity);
            }

            foreach (var activity in Activities)
            {
                if (activity.IsCritical)
                {
                    List<List<Activity>> allPaths = new List<List<Activity>>();
                    List<Activity> currentPath = new List<Activity>();

                    FindCriticalPaths(activity, currentPath, allPaths);

                    foreach (var path in allPaths)
                    {
                        criticalPaths.Add(path);
                    }
                }
            }

            List<List<Activity>> orderedCriticalPaths = new List<List<Activity>>();


            foreach (var path in criticalPaths)
            {
                List<Activity> lista = new List<Activity>();
                if (path[0].Name == "STOP")
                {
                    for (int i = path.Count - 1; i >= 0; i--)
                    {
                        var activity = path[i];
                        lista.Add(activity);
                    }
                    orderedCriticalPaths.Add(lista);
                }

            }

            ViewBag.CriticalPaths = orderedCriticalPaths;

            return View(Activities);

        }

        public async Task<IActionResult> Index(int? id, int? selectedId)
        {
            ViewBag.SelectedValue = selectedId;

            await CalculateCriticalPath(id);

            return View(Activities);
        }
    }
}
