using System;
using System.Collections.Generic;

namespace Licenta3.Models.ViewModels
{
    public class ResourceHistogramDetailed
    {
        public Resource Resource { get; set; }
        public Dictionary<int, decimal> Usage { get; set; } = new();
        public Dictionary<int, List<(string activityName, decimal qty)>> UsageDetails { get; set; } = new();
    }
}
