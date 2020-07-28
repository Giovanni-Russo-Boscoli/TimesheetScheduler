using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class JsonRatesAndRoles
    {

        [Required]
        public int Id { get; set; }

        [Required]
        public string ShortName { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        //[DisplayFormat(DataFormatString = "{0:0.##}")]
        //public string Rate { get; set; }
        public decimal Rate { get; set; }
    }
}