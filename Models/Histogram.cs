namespace Licenta3.Models
{
    public class ResourceHistogram
    {
        public Resource Resource { get; set; }
        public Dictionary<DateTime, decimal> Usage { get; set; } = new();
    }
}
