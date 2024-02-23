using Licenta3.Data;
using Licenta3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Licenta3.Controllers
{
	public class CriticalPath : Controller
	{
		private readonly ApplicationDbContext _context;
		List<Activity> Activities = new List<Activity>();

		public CriticalPath(ApplicationDbContext context)
		{
			_context = context;
		}


		public async Task<IActionResult> CalculateCriticalPath(int? id)
		{

			List<int> ES = new List<int>();
			List<int> LF = new List<int>();
			List<int> Slack = new List<int>();
			List<Activity> checkedActivities = new List<Activity>();
			decimal maxLF = 0;
			int STOPposition = 0;


			List<Models.Task> tasks = await _context.Tasks
										 .Where(t => t.ProjectId == id)
										 .ToListAsync();


			foreach (var task in tasks)
			{
				Activity activitate = new Activity(task.Id, task.Code, task.Name, task.Dependencies, decimal.Parse(task.Duration));
				Activities.Add(activitate);
			}

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
				if(numar>STOPposition)
						STOPposition = numar;

			}
		}

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
				if(activity.Inclusion == "-")
					finalActivities += activity.Code+",";
			}
			finalActivities = finalActivities.Remove(finalActivities.Length - 1);

			Activity FinalActivity=new Activity(0, "STOP", "STOP", finalActivities, 0, maxLF, maxLF, maxLF, maxLF, 0, true, "-", STOPposition+1);
			Activities.Add(FinalActivity);

			// Rezultate: Aveți activitățile critice și datele asociate calculate

			return View(Activities); // sau redirecționați către o altă acțiune sau pagină după calcul

		}

		public async Task<IActionResult> Index()
		{

			List<int> ES = new List<int>();
			List<int> LF = new List<int>();
			List<int> Slack = new List<int>();


			List<Models.Task> tasks = await _context.Tasks.ToListAsync();

			foreach (var task in tasks)
			{
				Activity activitate = new Activity(task.Id, task.Code, task.Name, task.Dependencies, decimal.Parse(task.Duration));
				Activities.Add(activitate);
			}


			// Pasul 2: Calcul Early Start (ES) pentru fiecare activitate
			foreach (var activity in Activities)
			{
				if (string.IsNullOrEmpty(activity.Dependencies) || activity.Dependencies == "-")
				{
					activity.EarlyStart = 0; // Activitatea de pornire
					activity.EarlyFinish = activity.Duration;
					activity.Position = 0;
				}
				else
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


			// Pasul 3: Calcul Late Finish (LF) pentru fiecare activitate
			// Parcurgeți activitățile în ordine inversă

			var LFT = Activities[Activities.Count - 1].EarlyFinish;


			for (int i = Activities.Count - 1; i >= 0; i--)
			{
				var activity = Activities[i];

				if (string.IsNullOrEmpty(activity.Inclusion) || activity.Inclusion == "-")
				{
					// Activitatea finală sau fără dependențe
					activity.LateFinish = LFT;
					activity.LateStart = LFT - activity.Duration;
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

			// Rezultate: Aveți activitățile critice și datele asociate calculate



			return View(Activities); // sau redirecționați către o altă acțiune sau pagină după calcul


		}
	}
}
