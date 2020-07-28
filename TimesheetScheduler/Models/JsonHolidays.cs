using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class JsonHolidays
    {

        [Required]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Description { get; set; }
    }
}