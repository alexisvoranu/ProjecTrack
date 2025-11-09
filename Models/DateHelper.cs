namespace Licenta3.Models
{
    public class DateHelper
    {
        public static DateTime AddTime(DateTime startingDate, double value, string um)
        {
            return um switch
            {
                "minute" => startingDate.AddMinutes(value),
                "ore" => startingDate.AddHours(value),
                "zile" => startingDate.AddDays(value),
                "săptămâni" => startingDate.AddDays(value * 7),
                "luni" => startingDate.AddMonths((int)value),
                "ani" => startingDate.AddYears((int)value),
                _ => startingDate
            };
        }

    }

}
