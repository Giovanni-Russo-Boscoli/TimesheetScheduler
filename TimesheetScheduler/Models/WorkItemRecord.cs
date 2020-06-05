using System;

namespace TimesheetScheduler.Models
{
    public class WorkItemRecord
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public int WorkItemNumber { get; set; }

        public string Description { get; set; }

        public float ChargeableHours { get; set; }

        public float NonChargeableHours { get; set; }

        public string Comments { get; set; }

        public bool IsWeekend { get; set; }
    }
}