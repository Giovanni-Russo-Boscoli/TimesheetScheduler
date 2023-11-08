using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class FiguresByTeamDivisionDTO
    {
        //public FiguresByTeamDivisionDTO()
        //{
        //    FiguresIndexes = new List<FiguresIndexesDTO>();
        //}

        public string TeamDivision { get; set; }
        //public IList<FiguresIndexesDTO> FiguresIndexes { get; set; }

        public string TotalExclVAT { get; set; }
        public string TotalInclVAT { get; set; }
    }
}