using System.Collections.Generic;

namespace Licenta3.Models.ViewModels
{
    // Reutilizăm ProjectResourceAllocationViewModel (care conține detalii per proiect)

    public class GlobalResourceAllocationViewModel
    {
        // Aceasta va fi lista care conține toate datele agregate, proiect cu proiect.
        public List<ProjectResourceAllocationViewModel> ProjectsAllocationData { get; set; }
    }
}