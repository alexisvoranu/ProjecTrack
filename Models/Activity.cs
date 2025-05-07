namespace Licenta3.Models
{
    public class Activity : Task
    {
        private decimal duration;
        private decimal earlyStart;
        private decimal lateStart;
        private decimal earlyFinish;
        private decimal lateFinish;
        private DateTime earlyStartDate;
        private DateTime earlyFinishDate;
        private DateTime lateFinishDate;
        private decimal slack;
        private bool isCritical;
        private string inclusion;
        private int position;

        public Activity() : base()
        {
        }

        public Activity(int id, string code, string name, string dependencies, decimal duration, 
            string measurementUnit, string state) : base (id, code, name, dependencies, measurementUnit, state)
        {
            Duration = duration;
        }

        public Activity(int id, string code, string name, string dependencies, 
            decimal duration, string measurementUnit, string state, decimal earlyStart, 
            decimal lateStart, decimal earlyFinish, decimal lateFinish, decimal slack, 
            bool isCritical, string inclusion, int position) : base(id, code, name, 
            dependencies, measurementUnit, state)
        {
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

        public decimal Duration { get => duration; set => duration = value; }
        public decimal EarlyStart { get => earlyStart; set => earlyStart = value; }
        public decimal LateStart { get => lateStart; set => lateStart = value; }
        public decimal EarlyFinish { get => earlyFinish; set => earlyFinish = value; }
        public decimal LateFinish { get => lateFinish; set => lateFinish = value; }
        public DateTime EarlyStartDate { get => earlyStartDate; set => earlyStartDate = value; }
        public DateTime EarlyFinishDate { get => earlyFinishDate; set => earlyFinishDate = value; }
        public DateTime LateFinishDate { get => lateFinishDate; set => lateFinishDate = value; }
        public decimal Slack { get => slack; set => slack = value; }
        public bool IsCritical { get => isCritical; set => isCritical = value; }
        public string Inclusion { get => inclusion; set => inclusion = value; }
        public int Position { get => position; set => position = value; }
    }
}
