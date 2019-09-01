using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Repository
{
    public interface ITimesheetSchedulerRepository : IDisposable
    {
        IEnumerable<TimesheetWorkItem> GetStudents();
        TimesheetWorkItem GetStudentByID(int studentId);
        void InsertStudent(TimesheetWorkItem student);
        void DeleteStudent(int studentID);
        void UpdateStudent(TimesheetWorkItem student);
        void Save();
    }
}
