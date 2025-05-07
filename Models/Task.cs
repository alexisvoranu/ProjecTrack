using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Task
    {
        [Key]
        protected int id;

        [Required]
        protected string code;

        [Required]
        protected string name;

        protected string? dependencies;

        [Required]
        protected string duration;
        [Required]
        protected string measurementUnit;
        protected string? state;

        [ForeignKey("Project")]
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        [ForeignKey("ApplicationUser")]
        public string UserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        protected DateTime? lateStartDate;

        public Task() 
        { 
        }

        public Task(int id, string code, string name, string? dependencies, string measurementUnit, string state)
        {
            this.id = id;
            this.code = code;
            this.name = name;
            this.dependencies = dependencies;
            this.measurementUnit = measurementUnit;
            this.state = state;
        }

        public int Id { get => id; set => id = value; }
        public string Code { get => code; set => code = value; }
        public string Name { get => name; set => name = value; }
        public string? Dependencies { get => dependencies; set => dependencies = value; }
        public string Duration { get => duration; set => duration = value; }
        public string MeasurementUnit { get => measurementUnit; set => measurementUnit = value; }
        public string? State { get => state; set => state = value; }
        public DateTime? LateStartDate { get => lateStartDate; set => lateStartDate = value; }

    }
}
