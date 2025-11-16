using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Task
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Code { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Name { get; set; }

        [Column(TypeName = "text")]
        public string? Dependencies { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Duration { get; set; }

        [Column(TypeName = "text")]
        public string? State { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime? LateStartDate { get; set; }

        public ICollection<TaskResource> TaskResources { get; set; } = new List<TaskResource>();

        public Task() { }
        public Task(int id, string code, string name, string? dependencies, string state) { Id = id; Code = code; Name = name; Dependencies = dependencies; State = state; }
    }
}
