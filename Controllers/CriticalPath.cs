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

        private void PushSuccessorsSimple(Activity parent, Dictionary<string, List<string>> successors, Dictionary<string, Activity> activityMap)
        {
            if (!successors.ContainsKey(parent.Code)) return;

            foreach (var succCode in successors[parent.Code])
            {
                if (activityMap.TryGetValue(succCode, out Activity child))
                {
                    if (parent.ScheduledFinish > child.ScheduledStart)
                    {
                        child.ScheduledStart = parent.ScheduledFinish;
                        child.ScheduledFinish = child.ScheduledStart + child.Duration;
                        PushSuccessorsSimple(child, successors, activityMap);
                    }
                }
            }
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
                        if (pred.EarlyFinish > maxPredecessorEF) maxPredecessorEF = pred.EarlyFinish;
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

            decimal projectDurationCPM = Activities.Any() ? Activities.Max(a => a.EarlyFinish) : 0;

            var visitedBackward = new HashSet<string>();
            void ComputeLateTimes(Activity act)
            {
                if (visitedBackward.Contains(act.Code)) return;
                decimal minSuccessorLS = projectDurationCPM;
                var validSuccessors = successors[act.Code].Where(s => activityMap.ContainsKey(s)).Select(s => activityMap[s]).ToList();
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
                0, "", "", projectDurationCPM, projectDurationCPM, projectDurationCPM, projectDurationCPM, 0, true, "-", 9999);
            stopActivity.EarlyStartDate = stopActivity.EarlyFinishDate = stopActivity.LateStartDate = stopActivity.LateFinishDate =
                DateHelper.AddTime(startingDate, (double)projectDurationCPM, um);
            activityMap["STOP"] = stopActivity;

            List<Activity> singleOptimalPath = new List<Activity>();
            Dictionary<string, int> maxDepthCache = new Dictionary<string, int>();

            int GetMaxDepth(string code)
            {
                if (maxDepthCache.ContainsKey(code)) return maxDepthCache[code];
                if (code == "STOP") return 0;
                var critSuccs = successors.ContainsKey(code) ? successors[code].Where(s => activityMap.ContainsKey(s) && activityMap[s].IsCritical).ToList() : new List<string>();
                if (!critSuccs.Any()) return 0;
                int max = 0;
                foreach (var s in critSuccs) { int d = GetMaxDepth(s); if (d > max) max = d; }
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
                if (nextStep != null) BuildOptimalPath(activityMap[nextStep]);
                else if (current.Code != "STOP") BuildOptimalPath(stopActivity);
            }

            var startNode = Activities.Where(a => a.IsCritical && a.Code != "STOP" &&
                    (!predecessors[a.Code].Any() || !predecessors[a.Code].Any(p => activityMap.ContainsKey(p) && activityMap[p].IsCritical)))
                .OrderByDescending(a => GetMaxDepth(a.Code)).FirstOrDefault();
            if (startNode != null) BuildOptimalPath(startNode);

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

                var preLevelingStarts = Activities.ToDictionary(a => a.Code, a => a.EarlyStart);
                decimal originalDuration = projectDurationCPM;

                var globalUsage = new Dictionary<string, Dictionary<int, decimal>>();

                void AddUsage(string resName, int start, int finish, decimal qty)
                {
                    if (!globalUsage.ContainsKey(resName)) globalUsage[resName] = new Dictionary<int, decimal>();
                    for (int t = start; t < finish; t++)
                    {
                        if (!globalUsage[resName].ContainsKey(t)) globalUsage[resName][t] = 0;
                        globalUsage[resName][t] += qty;
                    }
                }

                bool CheckAvailability(string resName, int start, int finish, decimal qtyNeeded, decimal limit)
                {
                    if (!globalUsage.ContainsKey(resName)) return true;
                    for (int t = start; t < finish; t++)
                    {
                        decimal current = globalUsage[resName].ContainsKey(t) ? globalUsage[resName][t] : 0;
                        if (current + qtyNeeded > limit) return false;
                    }
                    return true;
                }

                var criticalTasks = Activities.Where(a => a.IsCritical && a.Code != "STOP").ToList();
                foreach (var crt in criticalTasks)
                {
                    var resForTask = taskResources.Where(tr => tr.TaskId == crt.Id).ToList();
                    foreach (var tr in resForTask)
                        AddUsage(tr.Resource.Name, (int)crt.ScheduledStart, (int)crt.ScheduledFinish, tr.QuantityUsed);
                }

                var levelingQueue = Activities
                    .Where(a => !a.IsCritical && a.Code != "STOP")
                    .OrderBy(a => a.Slack)
                    .ThenByDescending(a => a.Duration)
                    .ToList();

                const int MAX_SHIFT = 100;

                foreach (var act in levelingQueue)
                {
                    var myResources = taskResources.Where(tr => tr.TaskId == act.Id).ToList();
                    bool positionFound = false;

                    for (int shift = 0; shift <= (int)act.Slack + MAX_SHIFT; shift++)
                    {
                        decimal tryStart = act.EarlyStart + shift;
                        decimal tryFinish = tryStart + act.Duration;
                        bool isFeasible = true;

                        if (predecessors.ContainsKey(act.Code))
                        {
                            foreach (var predCode in predecessors[act.Code])
                            {
                                if (activityMap.TryGetValue(predCode, out Activity pred))
                                {
                                    if (tryStart < pred.ScheduledFinish) { isFeasible = false; break; }
                                }
                            }
                        }

                        if (!isFeasible) continue;

                        foreach (var myRes in myResources)
                        {
                            decimal limit = resourceLimits.ContainsKey(myRes.Resource.Name) ? resourceLimits[myRes.Resource.Name] : 9999;

                            if (!CheckAvailability(myRes.Resource.Name, (int)tryStart, (int)tryFinish, myRes.QuantityUsed, limit))
                            {
                                isFeasible = false;
                                break;
                            }
                        }

                        if (isFeasible)
                        {
                            act.ScheduledStart = tryStart;
                            act.ScheduledFinish = tryFinish;

                            foreach (var myRes in myResources)
                            {
                                AddUsage(myRes.Resource.Name, (int)tryStart, (int)tryFinish, myRes.QuantityUsed);
                            }

                            PushSuccessorsSimple(act, successors, activityMap);
                            positionFound = true;

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

                List<string> impactReport = new List<string>();

                decimal newDuration = Activities.Max(a => a.ScheduledFinish);
                decimal totalDelay = newDuration - originalDuration;

                if (totalDelay > 0)
                {
                    impactReport.Add(
                        $"<div class='alert alert-danger shadow-sm border-0' role='alert' style='border-left: 5px solid #dc3545 !important;'>" +
                        $"  <h5 class='alert-heading mb-1'>⚠️ Impact asupra termenului limită!</h5>" +
                        $"  <p class='mb-0'>Durata a crescut cu <span class='badge badge-danger' style='font-size: 1em;'>{totalDelay} {um}</span> " +
                        $"  (Total nou: <strong>{newDuration} {um}</strong>)</p>" +
                        $"</div>"
                    );
                }
                else
                {
                    impactReport.Add(
                        $"<div class='alert alert-success shadow-sm border-0' role='alert' style='border-left: 5px solid #28a745 !important;'>" +
                        $"  <h5 class='alert-heading mb-1'>✅ Nivelare Reușită</h5>" +
                        $"  <p class='mb-0'>Resursele au fost optimizate fără a depăși termenul de <strong>{originalDuration} {um}</strong>.</p>" +
                        $"</div>"
                    );
                }

                bool changesFound = false;
                var changedActivities = Activities
                    .Where(a => preLevelingStarts.ContainsKey(a.Code) && (a.ScheduledStart - preLevelingStarts[a.Code] > 0))
                    .OrderBy(a => a.ScheduledStart)
                    .ToList();

                if (changedActivities.Any())
                {
                    impactReport.Add("<h6 class='text-secondary text-uppercase font-weight-bold mt-4 mb-3' style='font-size: 0.8rem; letter-spacing: 1px;'>Detaliere Modificări Activități</h6>");

                    foreach (var act in changedActivities)
                    {
                        changesFound = true;
                        decimal oldStart = preLevelingStarts[act.Code];
                        decimal shift = act.ScheduledStart - oldStart;

                        string borderClass = "";
                        string badgeClass = "";
                        string statusText = "";
                        string statusColor = "";

                        if (act.Slack == 0)
                        {
                            borderClass = "border-danger";
                            badgeClass = "badge-danger";
                            statusText = "Impact Critic (Slack 0)";
                            statusColor = "text-danger";
                        }
                        else if (shift > act.Slack)
                        {
                            borderClass = "border-warning";
                            badgeClass = "badge-warning text-dark";
                            statusText = $"A depășit rezerva ({act.Slack} {um})";
                            statusColor = "text-warning";
                        }
                        else
                        {
                            borderClass = "border-success";
                            badgeClass = "badge-success";
                            statusText = $"Acoperit de rezervă ({act.Slack} {um})";
                            statusColor = "text-success";
                        }

                        string itemHtml =
                            $"<div class='d-flex justify-content-between align-items-center bg-white p-3 mb-2 shadow-sm rounded border-left' style='border-left-width: 5px !important; border-left-color: {(act.Slack == 0 ? "#dc3545" : (shift > act.Slack ? "#ffc107" : "#28a745"))};'>" +
                            $"  <div>" +
                            $"      <h6 class='mb-0 font-weight-bold'>{act.Name}</h6>" +
                            $"      <div class='small text-muted mt-1'>" +
                            $"          Start: <span style='text-decoration: line-through;'>{oldStart}</span> ➜ <strong>{act.ScheduledStart}</strong>" +
                            $"      </div>" +
                            $"  </div>" +
                            $"  <div class='text-right'>" +
                            $"      <span class='badge {badgeClass} badge-pill' style='font-size: 0.9em'>+{shift} {um}</span>" +
                            $"      <div class='small {statusColor} mt-1 font-weight-bold' style='font-size: 0.75rem'>{statusText}</div>" +
                            $"  </div>" +
                            $"</div>";

                        impactReport.Add(itemHtml);
                    }
                }

                if (!changesFound)
                {
                    impactReport.Add(
                        "<div class='text-center p-4 text-muted bg-light rounded border border-light'>" +
                        "   <i class='fa fa-check-circle fa-2x mb-2'></i><br/>" +
                        "   Nu au fost necesare modificări. Planificarea inițială este validă din punct de vedere al resurselor." +
                        "</div>"
                    );
                }

                ViewBag.LevelingImpact = impactReport;
            }

            var taskResourcessFinal = await _context.TaskResources
                .Include(tr => tr.Resource).Include(tr => tr.Task)
                .Where(tr => tr.Task.ProjectId == id).ToListAsync();

            var resourceHistograms = new Dictionary<string, ResourceHistogram>();
            var overuseSummary = new Dictionary<string, List<string>>();

            foreach (var group in taskResourcessFinal.GroupBy(tr => tr.Resource))
            {
                var resource = group.Key;
                var entries = new List<(decimal start, decimal end, decimal qty)>();
                foreach (var tr in group)
                    if (activityMapById.TryGetValue(tr.TaskId, out Activity activity))
                        entries.Add((activity.ScheduledStart, activity.ScheduledFinish, tr.QuantityUsed));

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
                                detailsForMoment.Add(new TaskUsageDetail { TaskName = activity.Name, QuantityUsed = tr.QuantityUsed });
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
                    int startInt = overuses[0].interval; int previous = overuses[0].interval;
                    var cReq = overuses[0].required; var cAvail = overuses[0].available; var cOver = overuses[0].overused;

                    for (int i = 1; i < overuses.Count; i++)
                    {
                        var cur = overuses[i];
                        bool isSame = cur.required == cReq && cur.available == cAvail && cur.overused == cOver && cur.interval == previous + 1;
                        if (!isSame)
                        {
                            string lbl = (previous - startInt == 0) ? "intervalul" : "intervalele";
                            overuseList.Add($"În {lbl} {startInt} - {previous + 1}: Necesar {cReq:F2} vs Disponibil {cAvail:F2} (Depășire: {cOver:F2} {resource.MeasurementUnit})");
                            startInt = cur.interval; cReq = cur.required; cAvail = cur.available; cOver = cur.overused;
                        }
                        previous = cur.interval;
                    }
                    string fLbl = (previous - startInt == 0) ? "intervalul" : "intervalele";
                    overuseList.Add($"În {fLbl} {startInt} - {previous + 1}: Necesar {cReq:F2} vs Disponibil {cAvail:F2} (Depășire: {cOver:F2} {resource.MeasurementUnit})");
                }

                resourceHistograms[resource.Name] = new ResourceHistogram { Resource = resource, UsageDetails = usageDetails };
                if (overuseList.Any()) overuseSummary[resource.Name] = overuseList;
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
