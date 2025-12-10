using Licenta3.Data;
using Licenta3.Models;
using Licenta3.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Where(p => p.Id == id)
                .Select(p => new { p.StartingDate, p.MeasurementUnit })
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();

            DateTime startingDate = project.StartingDate;
            string um = project.MeasurementUnit;

            ViewBag.StartingDate = startingDate;
            ViewBag.Um = um;
            ViewBag.Id = id;
            ViewBag.MethodUsed = method;

            var dbTasks = await _context.Tasks
                .Where(t => t.ProjectId == id)
                .ToListAsync();

            var activityMap = new Dictionary<string, Activity>();
            var activityMapById = new Dictionary<int, Activity>();

            var predecessors = new Dictionary<string, List<string>>();
            var successors = new Dictionary<string, List<string>>();

            foreach (var task in dbTasks)
            {
                var act = new Activity(task.Id, task.Code, task.Name, task.Dependencies,
                                      decimal.Parse(task.Duration), um, task.State);

                Activities.Add(act);
                activityMap[task.Code] = act;
                activityMapById[task.Id] = act;

                predecessors[task.Code] = new List<string>();
                if (!successors.ContainsKey(task.Code)) successors[task.Code] = new List<string>();

                if (!string.IsNullOrEmpty(task.Dependencies) && task.Dependencies != "-")
                {
                    var deps = task.Dependencies.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    predecessors[task.Code].AddRange(deps);
                }
            }

            foreach (var kvp in predecessors)
            {
                string child = kvp.Key;
                foreach (var parent in kvp.Value)
                {
                    if (successors.ContainsKey(parent))
                        successors[parent].Add(child);
                    else
                        successors[parent] = new List<string> { child };
                }
            }

            var visitedForward = new HashSet<string>();

            void ComputeEarlyTimes(Activity act)
            {
                if (visitedForward.Contains(act.Code)) return;

                decimal maxPredecessorEF = 0;

                foreach (var predCode in predecessors[act.Code])
                {
                    if (activityMap.TryGetValue(predCode, out Activity pred))
                    {
                        ComputeEarlyTimes(pred);
                        if (pred.EarlyFinish > maxPredecessorEF)
                            maxPredecessorEF = pred.EarlyFinish;
                    }
                }

                act.EarlyStart = maxPredecessorEF;
                act.EarlyFinish = act.EarlyStart + act.Duration;

                act.ScheduledStart = act.EarlyStart;
                act.ScheduledFinish = act.EarlyFinish;

                act.EarlyStartDate = DateHelper.AddTime(startingDate, (double)act.EarlyStart, um);
                act.EarlyFinishDate = DateHelper.AddTime(startingDate, (double)act.EarlyFinish, um);

                visitedForward.Add(act.Code);
            }

            foreach (var act in Activities) ComputeEarlyTimes(act);

            decimal projectDuration = Activities.Any() ? Activities.Max(a => a.EarlyFinish) : 0;

            var visitedBackward = new HashSet<string>();

            void ComputeLateTimes(Activity act)
            {
                if (visitedBackward.Contains(act.Code)) return;

                decimal minSuccessorLS = projectDuration;

                var validSuccessors = successors[act.Code]
                    .Where(s => activityMap.ContainsKey(s))
                    .Select(s => activityMap[s])
                    .ToList();

                if (validSuccessors.Any())
                {
                    foreach (var succ in validSuccessors) ComputeLateTimes(succ);
                    minSuccessorLS = validSuccessors.Min(s => s.LateStart);
                }

                act.LateFinish = minSuccessorLS;
                act.LateStart = act.LateFinish - act.Duration;

                act.Slack = act.LateStart - act.EarlyStart;
                if (Math.Abs(act.Slack) < 0.001m) act.Slack = 0;

                act.IsCritical = (act.Slack == 0);

                act.LateStartDate = DateHelper.AddTime(startingDate, (double)act.LateStart, um);
                act.LateFinishDate = DateHelper.AddTime(startingDate, (double)act.LateFinish, um);

                visitedBackward.Add(act.Code);
            }

            foreach (var act in Activities) ComputeLateTimes(act);


            var predecessorsOfStop = Activities.Where(a => !successors[a.Code].Any()).Select(a => a.Code).ToList();
            Activity stopActivity = new Activity(0, "STOP", "STOP", string.Join(",", predecessorsOfStop),
                0, "", "", projectDuration, projectDuration, projectDuration, projectDuration, 0, true, "-", 9999);

            stopActivity.EarlyStartDate = stopActivity.EarlyFinishDate =
            stopActivity.LateStartDate = stopActivity.LateFinishDate =
                DateHelper.AddTime(startingDate, (double)projectDuration, um);

            activityMap["STOP"] = stopActivity;

            List<Activity> singleOptimalPath = new List<Activity>();
            Dictionary<string, int> maxDepthCache = new Dictionary<string, int>();

            int GetMaxDepth(string code)
            {
                if (maxDepthCache.ContainsKey(code)) return maxDepthCache[code];

                if (code == "STOP") return 0;

                var critSuccs = successors.ContainsKey(code)
                    ? successors[code].Where(s => activityMap.ContainsKey(s) && activityMap[s].IsCritical).ToList()
                    : new List<string>();

                if (!critSuccs.Any()) return 0;

                int max = 0;
                foreach (var s in critSuccs)
                {
                    int d = GetMaxDepth(s);
                    if (d > max) max = d;
                }

                maxDepthCache[code] = 1 + max;
                return 1 + max;
            }

            void BuildOptimalPath(Activity current)
            {
                singleOptimalPath.Add(current);
                if (current.Code == "STOP") return;

                var nextStep = successors[current.Code]
                    .Where(s => activityMap.ContainsKey(s) && activityMap[s].IsCritical)
                    .OrderByDescending(s => GetMaxDepth(s)) 
                    .ThenByDescending(s => activityMap[s].Duration)
                    .FirstOrDefault();

                if (nextStep != null)
                {
                    BuildOptimalPath(activityMap[nextStep]);
                }
                else if (current.Code != "STOP")
                {
                    BuildOptimalPath(stopActivity);
                }
            }

            var startNode = Activities
                .Where(a => a.IsCritical && a.Code != "STOP" &&
                    (!predecessors[a.Code].Any() || !predecessors[a.Code].Any(p => activityMap.ContainsKey(p) && activityMap[p].IsCritical)))
                .OrderByDescending(a => GetMaxDepth(a.Code))
                .FirstOrDefault();

            if (startNode != null)
            {
                BuildOptimalPath(startNode);
            }

            var finalPathsList = new List<List<Activity>>();
            if (singleOptimalPath.Any()) finalPathsList.Add(singleOptimalPath);
            ViewBag.CriticalPaths = finalPathsList;

            if (method == "Level")
            {
                var taskResources = await _context.TaskResources
                    .Include(tr => tr.Resource)
                    .Where(tr => tr.Task.ProjectId == id)
                    .ToListAsync();

                var resourceLimits = taskResources
                    .Select(tr => tr.Resource)
                    .Distinct()
                    .ToDictionary(r => r.Name, r => r.Quantity);

                var levelingQueue = Activities
                    .Where(a => !a.IsCritical && a.Code != "STOP")
                    .OrderBy(a => a.Slack)
                    .ThenByDescending(a => a.Duration)
                    .ToList();

                const int MAX_SHIFT = 100;

                foreach (var act in levelingQueue)
                {
                    for (int shift = 0; shift <= (int)act.Slack + MAX_SHIFT; shift++)
                    {
                        decimal tryStart = act.EarlyStart + shift;
                        decimal tryFinish = tryStart + act.Duration;
                        bool isFeasible = true;

                        var myResources = taskResources.Where(tr => tr.TaskId == act.Id).ToList();
                        if (!myResources.Any()) break;

                        foreach (var resLimit in resourceLimits)
                        {
                            string resName = resLimit.Key;
                            decimal limit = resLimit.Value;

                            for (decimal t = tryStart; t < tryFinish; t++)
                            {
                                decimal usageAtT = 0;
                                usageAtT += myResources.FirstOrDefault(r => r.Resource.Name == resName)?.QuantityUsed ?? 0;

                                foreach (var other in Activities.Where(a => a != act && a.Code != "STOP"))
                                {
                                    if (t >= other.ScheduledStart && t < other.ScheduledFinish)
                                    {
                                        var otherUsage = taskResources
                                            .FirstOrDefault(tr => tr.TaskId == other.Id && tr.Resource.Name == resName)
                                            ?.QuantityUsed ?? 0;
                                        usageAtT += otherUsage;
                                    }
                                }

                                if (usageAtT > limit)
                                {
                                    isFeasible = false;
                                    break;
                                }
                            }
                            if (!isFeasible) break;
                        }

                        if (isFeasible)
                        {
                            act.ScheduledStart = tryStart;
                            act.ScheduledFinish = tryFinish;

                            var dbTask = await _context.Tasks.FindAsync(act.Id);
                            if (dbTask != null)
                            {
                                dbTask.LateStartDate = DateHelper.AddTime(startingDate, (double)act.ScheduledStart, um);
                                _context.Tasks.Update(dbTask);
                            }
                            break;
                        }
                    }
                }
                await _context.SaveChangesAsync();

            }

            var taskResourcess = await _context.TaskResources
                .Include(tr => tr.Resource)
                .Include(tr => tr.Task)
                .Where(tr => tr.Task.ProjectId == id)
                .ToListAsync();

            var resourceHistograms = new Dictionary<string, ResourceHistogram>();
            var overuseSummary = new Dictionary<string, List<string>>();

            foreach (var group in taskResourcess.GroupBy(tr => tr.Resource))
            {
                var resource = group.Key;
                var entries = new List<(decimal start, decimal end, decimal qty)>();

                foreach (var tr in group)
                {
                    if (activityMapById.TryGetValue(tr.TaskId, out Activity activity))
                    {
                        entries.Add((activity.ScheduledStart, activity.ScheduledFinish, tr.QuantityUsed));
                    }
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
                        if (activityMapById.TryGetValue(tr.TaskId, out Activity activity))
                        {
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

            decimal finalProjectFinish = Activities.Max(a => a.ScheduledFinish);
            ViewBag.FinishingDate = DateHelper.AddTime(startingDate, (double)finalProjectFinish, um);

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
