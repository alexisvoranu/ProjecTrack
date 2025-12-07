using System.Collections.Generic;

namespace Licenta3.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public Dictionary<string, int> TaskStatusCounts { get; set; }

        public int TotalProjects { get; set; }
        public Dictionary<string, int> ProjectStatusCounts { get; set; }
    }
}