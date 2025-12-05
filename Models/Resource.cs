using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Resource
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Denumirea este obligatorie")]
        [Column(TypeName = "text")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Cantitatea este obligatorie")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cantitatea trebuie să fie pozitivă")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unitatea de măsură este obligatorie")]
        [Column(TypeName = "text")]
        public string MeasurementUnit { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        public ICollection<TaskResource> TaskResources { get; set; } = new List<TaskResource>();
    }
}
