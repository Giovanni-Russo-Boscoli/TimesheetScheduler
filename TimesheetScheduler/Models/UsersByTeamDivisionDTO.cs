using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class UsersByTeamDivisionDTO
    {
        public string TeamDivision { get; set; }

        public IList<JsonUser> TeamMembers { get; set; }
    }
}