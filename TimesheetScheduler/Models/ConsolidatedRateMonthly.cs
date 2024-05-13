using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class ConsolidatedRateMonthly
    {
        public string MemberName { get; set; }
        public decimal RateExcVat { get; set; }
        public decimal RateIncVat { get; set; }
        public double DaysWorked { get; set; }
        public decimal DayRateExcVat { get; set; }
        public decimal DayRateIncVat { get; set; }
        public string TeamDivision { get; set; }
        public string ProjectNameTFS { get; set; }
        public string Role { get; set; }
        public bool Chargeable { get; set; }
        public double ChargeableHours { get; set; }

    }
}