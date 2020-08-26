using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class JsonProjectIteration
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string ProjectNameTFS { get; set; }

        [Required]
        public string IterationPathTFS { get; set; }
    }
}