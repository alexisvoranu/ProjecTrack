using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Resource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Name { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string MeasurementUnit { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        public ICollection<TaskResource> TaskResources { get; set; } = new List<TaskResource>();
    }
}
