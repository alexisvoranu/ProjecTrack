using Licenta3.Models;

namespace Licenta3.Models.ViewModels
{
    public class ProjectDashboardViewModel
    {
        public Project Project { get; set; }
        public List<Licenta3.Models.Task> Tasks { get; set; }
    }
}