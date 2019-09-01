using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Repository
{
    public class TimesheetSchedulerContext : DbContext
    {
        public DbSet<TimesheetWorkItem> TimesheetWorkItem { get; set; }
        //public DbSet<Enrollment> Enrollments { get; set; }
        //public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}