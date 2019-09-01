using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class TimesheetWorkItem
    {
        public int TimesheetWorkItemId { get; set; }

        public bool IsWeekend { get; set; }

        public DateTime TimesheetDate { get; set; }

        public int WorkItemNumber { get; set; }

        public string Description { get; set; }

        public float ChargeableHours { get; set; }

        public float NonChargeableHours { get; set; }

        public string Comments { get; set; }

    }
}