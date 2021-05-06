using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class JsonProjectIteration
    {

        public JsonProjectIteration()
        {
            this.TeamDivision = new List<TeamDivision>();
        }

        [Required]
        public int Id { get; set; }

        [Required]
        public string ProjectNameTFS { get; set; }

        [Required]
        public string IterationPathTFS { get; set; }

        [Required]
        public string TeamName { get; set; }

        public IList<TeamDivision> TeamDivision { get; set; }
    }
}