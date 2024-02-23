namespace Licenta3.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Dependencies { get; set; } // Dependențe separate prin virgulă sau "-"
        public decimal Duration { get; set; }
        public decimal EarlyStart { get; set; }
        public decimal LateStart { get; set; }
        public decimal EarlyFinish { get; set; }
        public decimal LateFinish { get; set; }
        public decimal Slack { get; set; }
        public bool IsCritical { get; set; }
        public string Inclusion { get; set; }
        public int Position { get; set; }
        public Activity()
        {
        }
        public Activity(int id, string code, string name, string dependencies, decimal duration)
        {
            Id = id;
            Code = code;
            Name = name;
            Dependencies = dependencies;
            Duration = duration;
        }

        public Activity(int id, string code, string name, string dependencies, decimal duration, decimal earlyStart, decimal lateStart, decimal earlyFinish, decimal lateFinish, decimal slack, bool isCritical, string inclusion, int position)
        {
            Id = id;
            Code = code;
            Name = name;
            Dependencies = dependencies;
            Duration = duration;
            EarlyStart = earlyStart;
            LateStart = lateStart;
            EarlyFinish = earlyFinish;
            LateFinish = lateFinish;
            Slack = slack;
            IsCritical = isCritical;
            Inclusion = inclusion;
            Position = position;
        }
    }
}
