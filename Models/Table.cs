namespace Licenta3.Models
{
    public class Table
    {
        private string code;
        private int xs;
        private int ys;
        private int xf;
        private int yf;
        private bool isCritical;

        public Table(string code, int xS, int yS, int xF, int yF, bool IC)
        {
            this.Code = code;
            XS = xS;
            YS = yS;
            XF = xF;
            YF = yF;
            this.IsCritical = IC;
        }

        public string Code { get => code; set => code = value; }
        public int XS { get => xs; set => xs = value; }
        public int YS { get => ys; set => ys = value; }
        public int XF { get => xf; set => xf = value; }
        public int YF { get => yf; set => yf = value; }
        public bool IsCritical { get => isCritical; set => isCritical = value; }
    }
}
