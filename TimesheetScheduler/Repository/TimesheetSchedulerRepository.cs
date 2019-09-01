using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using TimesheetScheduler.Models;
using System.Data.Entity;

namespace TimesheetScheduler.Repository
{
    public class TimesheetSchedulerRepository : ITimesheetSchedulerRepository, IDisposable
    {
        private TimesheetSchedulerContext context;

        public TimesheetSchedulerRepository(TimesheetSchedulerContext context)
        {
            this.context = context;
        }

        public IEnumerable<TimesheetWorkItem> GetStudents()
        {
            return context.TimesheetWorkItem.ToList();
        }

        public TimesheetWorkItem GetStudentByID(int id)
        {
            return context.TimesheetWorkItem.Find(id);
        }

        public void InsertStudent(TimesheetWorkItem student)
        {
            context.TimesheetWorkItem.Add(student);
        }

        public void DeleteStudent(int studentID)
        {
            TimesheetWorkItem student = context.TimesheetWorkItem.Find(studentID);
            context.TimesheetWorkItem.Remove(student);
        }

        public void UpdateStudent(TimesheetWorkItem student)
        {
            context.Entry(student).State = EntityState.Modified;
        }

        public void Save()
        {
            context.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}