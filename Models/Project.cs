using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string MeasurementUnit { get; set; }

        [Column(TypeName = "text")]
        public string? UserId { get; set; }

        [Required]
        public DateTime StartingDate { get; set; }

        [Column(TypeName = "text")]
        public string? State { get; set; }
    }
}
