using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.ViewModel
{
    public class UserDataSearchTFS
    {
        public string UserName { get; set; }
        public string ProjectNameTFS { get; set; }
        public string IterationPathTFS { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}