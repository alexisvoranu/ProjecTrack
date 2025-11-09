using Licenta3.Data;
using Licenta3.Models;
using Licenta3.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

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

        public async Task<IActionResult> Details(int id, string method)
        {
            int projectId = await _context.Tasks
                .Where(t => t.Id == id)
                .Select(t => t.ProjectId)
                .FirstOrDefaultAsync();

            if (projectId == 0)
                return NotFound();

            await CalculateCriticalPath(projectId, method);

            var activity = Activities.Find(x => x.Id == id);
            if (activity == null)
                return NotFound();

            string um = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.MeasurementUnit)
                .FirstOrDefaultAsync();
            ViewBag.Um = um;

            List<ValueTuple<string, string>> dependenciesList = new List<ValueTuple<string, string>>();

            if (!string.IsNullOrEmpty(activity.Dependencies) && activity.Dependencies != "-")
            {
                var dependencyCodes = activity.Dependencies
                    .Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var dependenciesData = await _context.Tasks
                    .Where(t => t.ProjectId == projectId)
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
                .Where(r => r.TaskId == id)
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

            return View(activity);
        }

        public async Task<IActionResult> CalculateCriticalPath(int? id, string method)
        {

            // PASUL 1: CALCUL CPM 
            List<Activity> checkedActivities = new List<Activity>();
            decimal maxLF = 0;
            int STOPposition = 0;

            List<Models.Task> tasks = await _context.Tasks
                                                    .Where(t => t.ProjectId == id)
                                                    .ToListAsync();

            DateTime startingDate = await _context.Projects
                                                    .Where(t => t.Id == id)
                                                    .Select(t => t.StartingDate)
                                                    .FirstOrDefaultAsync();

            string um = await _context.Projects
                                        .Where(t => t.Id == id)
                                        .Select(t => t.MeasurementUnit)
                                        .FirstOrDefaultAsync();

            ViewBag.StartingDate = startingDate;
            ViewBag.Um = um;

            foreach (var task in tasks)
            {
                Activity activitate = new Activity(task.Id, task.Code, task.Name, task.Dependencies, decimal.Parse(task.Duration), um, task.State);
                Activities.Add(activitate);
            }

            foreach (var activity in Activities)
            {
                if (activity.Dependencies == "-")
                {
                    activity.EarlyStart = 0;
                    activity.EarlyStartDate = startingDate;
                    activity.EarlyFinish = activity.Duration;
                    activity.EarlyFinishDate = DateHelper.AddTime(startingDate, (double)activity.Duration, um);
                    activity.Position = 0;
                    checkedActivities.Add(activity);
                }
            }

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

            foreach (var activity in Activities)
            {
                if (activity.Dependencies != "-")
                {
                    var dependencyIds = activity.Dependencies.Split(',');
                    decimal maxDependencyEF = 0;

                    foreach (var idStr in dependencyIds)
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (dependentActivity != null)
                            {
                                maxDependencyEF = Math.Max(maxDependencyEF, dependentActivity.EarlyFinish);
                            }
                        }
                    }

                    activity.EarlyStart = maxDependencyEF;
                    activity.EarlyFinish = maxDependencyEF + activity.Duration;
                    activity.EarlyStartDate = DateHelper.AddTime(startingDate, (double)maxDependencyEF, um);
                    activity.EarlyFinishDate = DateHelper.AddTime(startingDate, (double)maxDependencyEF + (double)activity.Duration, um);

                    if (activity.EarlyFinish > maxLF)
                        maxLF = activity.EarlyFinish;

                    int maxPosition = 0;

                    foreach (var idStr in dependencyIds)
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

            for (int i = Activities.Count - 1; i >= 0; i--)
            {
                var activity = Activities[i];

                if (string.IsNullOrEmpty(activity.Inclusion) || activity.Inclusion == "-")
                {
                    activity.LateFinish = maxLF;
                    activity.LateFinishDate = DateHelper.AddTime(startingDate, (double)maxLF, um);
                    activity.LateStart = activity.LateFinish - activity.Duration;
                    activity.LateStartDate = DateHelper.AddTime(startingDate, (double)activity.LateFinish
                        - (double)activity.Duration, um);
                }
                else
                {
                    var inclusionIds = activity.Inclusion.Split(',');
                    decimal mininclusionLS = decimal.MaxValue;

                    foreach (var idStr in inclusionIds)
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            var inclusionActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (inclusionActivity != null)
                            {
                                mininclusionLS = Math.Min(mininclusionLS, inclusionActivity.LateStart);
                            }
                        }
                    }

                    activity.LateFinish = mininclusionLS;
                    activity.LateFinishDate = DateHelper.AddTime(startingDate, (double)mininclusionLS, um);
                    activity.LateStart = activity.LateFinish - activity.Duration;
                    activity.LateStartDate = DateHelper.AddTime(startingDate, (double)mininclusionLS
                        - (double)activity.Duration, um);
                }

                activity.Slack = activity.LateStart - activity.EarlyStart;
                if (activity.Slack == 0)
                {
                    activity.IsCritical = true;
                }
                activity.ScheduledStart = activity.EarlyStart;
                activity.ScheduledFinish = activity.EarlyFinish;
            }

            //Calcul Slack si identificarea activitatilor critice
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
            FinalActivity.EarlyStartDate = DateHelper.AddTime(startingDate, (double)maxLF, um);
            FinalActivity.EarlyFinishDate = DateHelper.AddTime(startingDate, (double)maxLF, um);
            FinalActivity.LateStartDate = DateHelper.AddTime(startingDate, (double)maxLF, um);
            FinalActivity.LateFinishDate = DateHelper.AddTime(startingDate, (double)maxLF, um);
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
                case "săptămâni":
                    finishingDate = startingDate.AddDays((double)maxLF * 7);
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


            // PASII 2 SI 3: NIVELAREA SI PROPAGAREA
            if (method == "Level")
            {
                var taskResources = await _context.TaskResources
                    .Include(tr => tr.Resource)
                    .Include(tr => tr.Task)
                    .Where(tr => tr.Task.ProjectId == id)
                    .ToListAsync();

                var resources = taskResources.Select(tr => tr.Resource).Distinct().ToDictionary(r => r.Name, r => r.Quantity);

                // PRIORITATE
                // 1. Activiti non-critice
                // 2. Sortare primara: după Slack (cel mai mic primul)
                // 3. Sortare secundara (Tie-breaker): dupa durata (cea mai mare prima)
                var nonCriticalActivities = Activities
                    .Where(a => !a.IsCritical && a.Code != "STOP")
                    .OrderBy(a => a.Slack)
                    .ThenByDescending(a => a.Duration)
                    .ToList();

                const decimal MAX_SHIFT_LIMIT = 100;

                foreach (var activityToLevel in nonCriticalActivities)
                {
                    for (decimal offset = 0; offset < (activityToLevel.Slack + MAX_SHIFT_LIMIT); offset++)
                    {
                        decimal potentialSS = activityToLevel.EarlyStart + offset;
                        decimal potentialSF = potentialSS + activityToLevel.Duration;

                        bool isFeasible = true;

                        foreach (var resource in resources)
                        {
                            if (!isFeasible) break;

                            string resourceName = resource.Key;
                            decimal maxAvailable = resource.Value;

                            for (int t = (int)potentialSS; t < (int)potentialSF; t++)
                            {
                                decimal totalRequired = 0;

                                foreach (var currentActivity in Activities.Where(a => a.Code != "STOP"))
                                {
                                    decimal currentSS, currentSF;

                                    if (currentActivity.Id == activityToLevel.Id)
                                    {
                                        currentSS = potentialSS;
                                        currentSF = potentialSF;
                                    }
                                    else
                                    {
                                        currentSS = currentActivity.ScheduledStart;
                                        currentSF = currentActivity.ScheduledFinish;
                                    }

                                    if (t >= currentSS && t < currentSF)
                                    {
                                        var currentResourceUsage = taskResources
                                            .FirstOrDefault(tr => tr.TaskId == currentActivity.Id && tr.Resource.Name == resourceName);

                                        if (currentResourceUsage != null)
                                        {
                                            totalRequired += currentResourceUsage.QuantityUsed;
                                        }
                                    }
                                }

                                if (totalRequired > maxAvailable)
                                {
                                    isFeasible = false;
                                    break;
                                }
                            }

                            if (!isFeasible) break;
                        }

                        if (isFeasible)
                        {
                            activityToLevel.ScheduledStart = potentialSS;
                            activityToLevel.ScheduledFinish = potentialSF;

                            var task = await _context.Tasks.Where(t => t.Id == activityToLevel.Id).FirstOrDefaultAsync();
                            if (task != null)
                            {
                                task.LateStartDate = DateHelper.AddTime(startingDate, (double)potentialSS, um);
                                _context.Tasks.Update(task);
                            }

                            break;
                        }
                    }
                }

                decimal newMaxSF = Activities.Max(a => a.ScheduledFinish);


                foreach (var activity in Activities.OrderBy(a => a.Position))
                {
                    if (activity.Dependencies != "-")
                    {
                        var dependencyCodes = activity.Dependencies.Split(',');
                        decimal maxDependencySF = 0;

                        foreach (var code in dependencyCodes)
                        {
                            if (string.IsNullOrWhiteSpace(code)) continue;

                            var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(code));

                            if (dependentActivity != null)
                            {
                                maxDependencySF = Math.Max(maxDependencySF, dependentActivity.ScheduledFinish);
                            }
                        }

                        if (maxDependencySF > activity.EarlyStart)
                        {
                            activity.EarlyStart = maxDependencySF;
                            activity.EarlyFinish = maxDependencySF + activity.Duration;

                            activity.ScheduledStart = maxDependencySF;
                            activity.ScheduledFinish = activity.EarlyFinish;

                            decimal shift = activity.EarlyStart - activity.LateStart;
                            activity.LateStart += shift;
                            activity.LateFinish += shift;
                            activity.Slack = activity.LateStart - activity.EarlyStart;

                            if (activity.ScheduledStart == activity.LateStart)
                            {
                                activity.IsCritical = true;
                            }
                        }
                    }
                }
            }

            ViewBag.MethodUsed = method;
            ViewBag.ProjectId = id;

            // PASUL 4: CALCULUL HISTOGRAMEI

            var taskResourcess = await _context.TaskResources
                .Include(tr => tr.Resource)
                .Include(tr => tr.Task)
                .Where(tr => tr.Task.ProjectId == id)
                .ToListAsync();

            ViewBag.Um = um;

            var resourceHistograms = new Dictionary<string, ResourceHistogram>();
            var overuseSummary = new Dictionary<string, List<string>>();

            foreach (var group in taskResourcess.GroupBy(tr => tr.Resource))
            {
                var resource = group.Key;
                var entries = new List<(decimal start, decimal end, decimal qty)>();

                foreach (var tr in group)
                {
                    var activity = Activities.FirstOrDefault(a => a.Id == tr.TaskId);
                    if (activity == null) continue;

                    entries.Add((activity.ScheduledStart, activity.ScheduledFinish, tr.QuantityUsed));
                }

                if (!entries.Any()) continue;

                var start = entries.Min(e => e.start);
                var end = entries.Max(e => e.end);

                var usageDetails = new Dictionary<int, List<TaskUsageDetail>>();
                var overuses = new List<(int interval, decimal required, decimal available, decimal overused)>();

                for (int t = (int)start; t <= (int)end; t++)
                {
                    var detailsForMoment = new List<TaskUsageDetail>();
                    decimal totalForMoment = 0;

                    foreach (var tr in group)
                    {
                        var activity = Activities.FirstOrDefault(a => a.Id == tr.TaskId);
                        if (activity == null) continue;

                        if (t >= activity.ScheduledStart && t < activity.ScheduledFinish)
                        {
                            detailsForMoment.Add(new TaskUsageDetail
                            {
                                TaskName = activity.Name,
                                QuantityUsed = tr.QuantityUsed
                            });
                            totalForMoment += tr.QuantityUsed;
                        }
                    }

                    if (detailsForMoment.Any())
                    {
                        usageDetails[t] = detailsForMoment;

                        if (totalForMoment > resource.Quantity)
                        {
                            decimal diff = totalForMoment - resource.Quantity;
                            overuses.Add((t, totalForMoment, resource.Quantity, diff));
                        }
                    }
                }

                var overuseList = new List<string>();

                if (overuses.Any())
                {
                    int startInt = overuses[0].interval;
                    int previous = overuses[0].interval;
                    var currentRequired = overuses[0].required;
                    var currentAvailable = overuses[0].available;
                    var currentOverused = overuses[0].overused;

                    for (int i = 1; i < overuses.Count; i++)
                    {
                        var current = overuses[i];

                        bool isSame = current.required == currentRequired &&
                                                      current.available == currentAvailable &&
                                                      current.overused == currentOverused &&
                                                      current.interval == previous + 1;

                        if (!isSame)
                        {
                            string label = (previous - startInt == 0) ? "intervalul" : "intervalele";
                            overuseList.Add($"În {label} {startInt} - {previous + 1}: " +
                                                    $"Necesarul total ({currentRequired:F2} {resource.MeasurementUnit}) depășește disponibilul ({currentAvailable:F2}) cu {currentOverused:F2} {resource.MeasurementUnit}");

                            startInt = current.interval;
                            currentRequired = current.required;
                            currentAvailable = current.available;
                            currentOverused = current.overused;
                        }

                        previous = current.interval;
                    }

                    string finalLabel = (previous - startInt == 0) ? "intervalul" : "intervalele";
                    overuseList.Add($"În {finalLabel} {startInt} - {previous + 1}: " +
                                            $"Necesarul total ({currentRequired:F2} {resource.MeasurementUnit}) depășește disponibilul ({currentAvailable:F2}) cu {currentOverused:F2} {resource.MeasurementUnit}");

                }

                resourceHistograms[resource.Name] = new ResourceHistogram
                {
                    Resource = resource,
                    UsageDetails = usageDetails
                };

                if (overuseList.Any())
                    overuseSummary[resource.Name] = overuseList;
            }

            ViewBag.ResourceHistograms = resourceHistograms;
            ViewBag.OveruseSummary = overuseSummary;

            foreach (var activity in Activities)
            {
                activity.EarlyStartDate = DateHelper.AddTime(startingDate, (double)activity.ScheduledStart, um);
                activity.EarlyFinishDate = DateHelper.AddTime(startingDate, (double)activity.ScheduledFinish, um);
                activity.LateStartDate = DateHelper.AddTime(startingDate, (double)activity.LateStart, um);
                activity.LateFinishDate = DateHelper.AddTime(startingDate, (double)activity.LateFinish, um);
            }

            decimal newProjectFinish = Activities.Max(a => a.ScheduledFinish);
            finishingDate = DateHelper.AddTime(startingDate, (double)newProjectFinish, um);
            ViewBag.FinishingDate = finishingDate;

            return View(Activities);
        }

        public async Task<IActionResult> Index(int id, int? selectedId, string method)
        {
            ViewBag.SelectedValue = selectedId;

            await CalculateCriticalPath(id, method);

            return View(Activities);
        }

        [HttpGet]
        public async Task<IActionResult> OptimizedSchedule(int id, [FromQuery] string method)
        {
            ViewBag.Id = id;
            return await CalculateCriticalPath(id, method);
        }
    }
}
