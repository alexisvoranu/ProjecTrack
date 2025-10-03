using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta3.Models
{
    public class Resource
    {
        [Key]
        protected int id;

        [Required]
        protected string name;

        [Required]
        protected decimal quantity;

        [Required]
        protected string measurementUnit;

        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        public Resource()
        {
        }

        public Resource(int id, string name, decimal quantity, string measurementUnit)
        {
            this.id = id;
            this.name = name;
            this.quantity = quantity;
            this.measurementUnit = measurementUnit;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public decimal Quantity { get => quantity; set => quantity = value; }
        public string MeasurementUnit { get => measurementUnit; set => measurementUnit = value; }
       
    }
}
