using Microsoft.AspNetCore.Mvc.Rendering;

namespace Licenta3.Models.ViewModels
{
    public class TaskResourceManageViewModel
    {
        public Task Task { get; set; } = null!;
        public List<TaskResource> AssignedResources { get; set; } = new();
        public List<SelectListItem> AvailableResources { get; set; } = new();
    }
}
