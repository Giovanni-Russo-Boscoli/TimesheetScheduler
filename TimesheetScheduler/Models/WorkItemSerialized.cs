using System;

namespace TimesheetScheduler.Models
{
    public class WorkItemSerialized
    {

        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public string Description { get; set; }
        public double? CompletedHours { get; set; }
        public string WorkItemsLinked { get; set; }
        public string State { get; set; }
        public string LinkUrl { get; set; }

    }
}