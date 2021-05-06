using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class JsonVAT
    {
        public int Id { get; set; }
        public decimal VAT { get; set; }
        public string VATText { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime? EndPeriod { get; set; }
        public bool Active { get; set; }
    }
}