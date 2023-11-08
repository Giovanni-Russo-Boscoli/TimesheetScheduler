using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class FiguresDTO
    {
        public FiguresDTO()
        {
            FiguresByTeamDivision = new List<FiguresByTeamDivisionDTO>();
        }
        public string TeamName { get; set; }
        //public string PeriodSearched { get; set; }
        public string VatApplied { get; set; }
        public string TotalExclVat { get; set; }
        public string TotalInclVat { get; set; }
        
        public IList<FiguresByTeamDivisionDTO> FiguresByTeamDivision { get; set; }
        public IList<ConsolidatedRateMonthly> Members { get; set; }

        public DateTime PeriodSearched { get; set; }
        
        public string PeriodSearchedString { get { return PeriodSearched.ToString("MMMM yyyy"); } }

        public string PeriodSearchedFullDate()
        {
            return $"1st {PeriodSearched.ToString("MMMM")} to {formatDayString(DateTime.DaysInMonth(PeriodSearched.Year, PeriodSearched.Month))} {PeriodSearched.ToString("MMMM")} {PeriodSearched.Year}";
        }

        private string formatDayString(int day)
        {
            switch(day)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                case 21: return "21st";
                case 22: return "22nd";
                case 23: return "23rd";
                case 31: return "31st";
                default: return $"{day}th";
            }
        }
    }
}