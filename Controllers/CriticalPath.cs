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
        private static List<Activity> ActivitiesDetails = new();

        public CriticalPath(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int id)
        {

            var activity = ActivitiesDetails[id-1];

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
                ActivitiesDetails.Add(activitate);
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
                    // Activitatea finala sau fara dependențe
                    activity.LateFinish = maxLF;
                    activity.LateFinishDate = startingDate.AddDays((double)maxLF);
                    activity.LateStart = activity.LateFinish - activity.Duration;
                    activity.LateStartDate = startingDate.AddDays((double)activity.LateFinish
                        - (double)activity.Duration);
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
            List<int> ES = new List<int>();
            List<int> LF = new List<int>();
            List<int> Slack = new List<int>();
            List<Activity> checkedActivities = new List<Activity>();
            string um = "";
            ViewBag.SelectedValue = selectedId;

            decimal maxLF = 0;
            int STOPposition = 0;


            List<Models.Task> tasks = await _context.Tasks
                                         .Where(t => t.ProjectId == id)
                                         .ToListAsync();

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
                    activity.EarlyFinish = activity.Duration;
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
                    // Calculează ES bazat pe dependențe multiple
                    var dependencyIds = activity.Dependencies.Split(','); // Split după virgulă

                    decimal maxDependencyES = 0;

                    foreach (var idStr in dependencyIds)
                    {

                        if (!string.IsNullOrEmpty(idStr)) // Verifică dacă ID-ul nu este gol
                        {
                            // Cauta activitatea cu ID-ul dat
                            var dependentActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (dependentActivity != null)
                            {
                                // Verificăm și actualizăm Early Start
                                maxDependencyES = Math.Max(maxDependencyES, dependentActivity.EarlyFinish);
                            }
                            else
                            {
                                // Tratare caz în care ID-ul nu corespunde nicio activitate
                            }

                        }
                    }


                    activity.EarlyStart = maxDependencyES;
                    activity.EarlyFinish = maxDependencyES + activity.Duration;

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
            // Parcurgeți activitățile în ordine inversă

            var LFT = Activities[Activities.Count - 1].EarlyFinish;


            for (int i = Activities.Count - 1; i >= 0; i--)
            {
                var activity = Activities[i];


                if (string.IsNullOrEmpty(activity.Inclusion) || activity.Inclusion == "-")
                {
                    // Activitatea finală sau fără dependențe
                    activity.LateFinish = maxLF;
                    activity.LateStart = activity.LateFinish - activity.Duration;
                }
                else
                {
                    // Calculează LF bazat pe dependențe

                    var inclusionIds = activity.Inclusion.Split(',');
                    decimal mininclusionLF = decimal.MaxValue; // Inițializați cu o valoare mare pentru a găsi minimul corect

                    foreach (var idStr in inclusionIds)
                    {
                        if (!string.IsNullOrEmpty(idStr)) // Verifică dacă ID-ul nu este gol
                        {
                            // Cauta activitatea cu ID-ul dat
                            var inclusionActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            if (inclusionActivity != null)
                            {
                                // Verificăm și actualizăm Late Finish
                                mininclusionLF = Math.Min(mininclusionLF, inclusionActivity.LateStart);
                            }
                            else
                            {
                                // Tratare caz în care ID-ul nu corespunde nicio activitate
                            }
                        }
                    }

                    activity.LateFinish = mininclusionLF;
                    activity.LateStart = activity.LateFinish - activity.Duration;

                }
            }



            // Pasul 4: Calcul Slack și identificarea activităților critice
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

            Activity FinalActivity = new Activity(0, "STOP", "STOP", finalActivities, 0, "", "",
                maxLF, maxLF, maxLF, maxLF, 0, true, "-", STOPposition + 1);
            Activities.Add(FinalActivity);

            // Rezultate: Aveți activitățile critice și datele asociate calculate

            ViewBag.Id = id;

            List<List<Activity>> criticalPaths = new List<List<Activity>>();
            // Funcție recursivă pentru a găsi toate căile critice în graf
            void FindCriticalPaths(Activity currentActivity, List<Activity> currentPath, List<List<Activity>> allPaths)
            {
                // Adăugați activitatea curentă la calea curentă
                currentPath.Add(currentActivity);

                // Dacă activitatea curentă nu are dependențe, aceasta este considerată ultima activitate din proiect
                if (currentActivity.Dependencies == "-")
                {
                    // Adăugați calea curentă la lista de căi critice
                    allPaths.Add(new List<Activity>(currentPath));
                }
                else
                {
                    // Altfel, iterați prin toate activitățile următoare
                    var dependencyIds = currentActivity.Dependencies.Split(',');

                    foreach (var idStr in dependencyIds)
                    {
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            // Căutați activitatea cu ID-ul dat
                            var nextActivity = Activities.FirstOrDefault(a => a.Code.Equals(idStr));

                            // Verificați dacă activitatea următoare este pe calea critică și nu a fost vizitată anterior în această cale, continuați căutarea
                            if (nextActivity != null && nextActivity.IsCritical && !currentPath.Contains(nextActivity))
                            {
                                FindCriticalPaths(nextActivity, currentPath, allPaths);
                            }
                        }
                    }
                }

                // Eliminați activitatea curentă din calea curentă pentru a explora alte căi
                currentPath.Remove(currentActivity);
            }

            // Identificați și salvați căile critice
            foreach (var activity in Activities)
            {
                // Verificați doar activitățile care sunt critice și care nu au fost vizitate anterior
                if (activity.IsCritical)
                {
                    List<List<Activity>> allPaths = new List<List<Activity>>();
                    List<Activity> currentPath = new List<Activity>();

                    // Găsiți toate căile critice pentru activitatea curentă
                    FindCriticalPaths(activity, currentPath, allPaths);

                    // Adăugați toate căile critice găsite la lista generală de căi critice
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

            return View(Activities); // sau redirecționați către o altă acțiune sau pagină după calcul
        }
    }
}
