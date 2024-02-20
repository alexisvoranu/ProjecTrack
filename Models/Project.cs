using System.ComponentModel.DataAnnotations;

namespace Licenta3.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? UserId { get; set; }
    }
}
