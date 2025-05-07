using System.ComponentModel.DataAnnotations;

namespace Licenta3.Models
{
    public class Project
    {
        [Key]
        private int id;
        [Required]
        private string name;
        private string? userId;
        [Required]
        private DateTime startingDate;
        private string? state;

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string? UserId { get => userId; set => userId = value; }
        public DateTime StartingDate { get => startingDate; set => startingDate = value; }
        public string? State { get => state; set => state = value; }
    }
}
