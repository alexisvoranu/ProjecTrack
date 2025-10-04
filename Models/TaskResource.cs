using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class TaskResource
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Task")]
        public int TaskId { get; set; }
        public Task Task { get; set; }

        [ForeignKey("Resource")]
        public int ResourceId { get; set; }
        public Resource Resource { get; set; }

        public decimal QuantityUsed { get; set; }
    }
}
