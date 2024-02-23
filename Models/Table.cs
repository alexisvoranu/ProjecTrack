namespace Licenta3.Models
{
    public class Table
    {
        public string code { get; set; }
        public int XS { get; set; }
        public int YS { get; set; }
        public int XF { get; set; }
        public int YF { get; set; }
        public bool IsCritical { get; set; }

        public Table(string code, int xS, int yS, int xF, int yF, bool IC)
        {
            this.code = code;
            XS = xS;
            YS = yS;
            XF = xF;
            YF = yF;
            this.IsCritical = IC;
        }
    }
}
