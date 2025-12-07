using Licenta3.Models;
using System.Collections.Generic;

namespace Licenta3.Models.ViewModels
{
    // Acestea se presupun definite în Licenta3.Models.ViewModels

    public class TaskResourceDetail
    {
        public int TaskId { get; set; }
        public string TaskCode { get; set; }
        public string TaskName { get; set; }
        // Detaliile resursei alocate, unde QuantityUsed provine din TaskResource
        public List<TaskResource> AllocatedResources { get; set; }
    }

    public class ProjectResourceAllocationViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        // 1. Resursele disponibile pentru întregul proiect (Model Resource)
        public List<Resource> AvailableResources { get; set; }

        // 2. Resursele necesare/alocate, grupate pe activități (Task-uri)
        public List<TaskResourceDetail> TaskAllocations { get; set; }
    }
}