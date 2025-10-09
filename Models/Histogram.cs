namespace Licenta3.Models
{
    public class TaskUsageDetail
    {
        public string TaskName { get; set; }
        public decimal QuantityUsed { get; set; }
    }
    public class ResourceHistogram
    {
        public Resource Resource { get; set; }
        public Dictionary<DateTime, decimal> Usage { get; set; } = new();
        public IDictionary<int, List<TaskUsageDetail>> UsageDetails { get; set; }
    }
}
