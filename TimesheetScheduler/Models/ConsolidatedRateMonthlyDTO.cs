using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class ConsolidatedRateMonthlyDTO
    {
        public ConsolidatedRateMonthlyDTO()
        {
            FiguresIndexes = new List<FiguresIndexesDTO>();
        }

        public IList<ConsolidatedRateMonthly> Members { get; set; }

        public IList<FiguresIndexesDTO> FiguresIndexes { get; set; }
    }
}