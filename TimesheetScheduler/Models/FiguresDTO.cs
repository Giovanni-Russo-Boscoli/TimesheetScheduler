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
        public string PeriodSearched { get; set; }
        public string VatApplied { get; set; }
        public string TotalExclVat { get; set; }
        public string TotalInclVat { get; set; }
        public IList<FiguresByTeamDivisionDTO> FiguresByTeamDivision { get; set; }
        public IList<ConsolidatedRateMonthly> Members { get; set; }
    }
}