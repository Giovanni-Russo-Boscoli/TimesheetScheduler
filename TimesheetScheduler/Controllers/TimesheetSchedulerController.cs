using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimesheetScheduler.Models;
using TimesheetScheduler.Repository;

namespace TimesheetScheduler.Controllers
{
    public class TimesheetSchedulerController : Controller
    {
        private ITimesheetSchedulerRepository timesheetSchedulerRepository;

        public TimesheetSchedulerController()
        {
            this.timesheetSchedulerRepository = new TimesheetSchedulerRepository(new TimesheetSchedulerContext());
        }

        public TimesheetSchedulerController(ITimesheetSchedulerRepository timesheetSchedulerRepository)
        {
            this.timesheetSchedulerRepository = timesheetSchedulerRepository;
        }


        // GET: TimesheetScheduler
        public ActionResult Index()
        {
            var a = timesheetSchedulerRepository.GetStudents();
            var b = a.Where(x => x.WorkItemNumber == 354123).FirstOrDefault();
            b.Comments = "Edited comment";
            timesheetSchedulerRepository.UpdateStudent(b);

            TimesheetWorkItem newObj = new TimesheetWorkItem();
            newObj.Comments = "Comments...";
            newObj.Description = "DEeeeeescription";
            newObj.TimesheetDate = DateTime.Now;
            timesheetSchedulerRepository.InsertStudent(newObj);
            timesheetSchedulerRepository.Save();
            return View();
        }

        // GET: TimesheetScheduler/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TimesheetScheduler/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TimesheetScheduler/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here0

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TimesheetScheduler/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TimesheetScheduler/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TimesheetScheduler/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TimesheetScheduler/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public ActionResult TestAutentication()
        {
            Console.Write("AuthenticationType:" + User.Identity.AuthenticationType);
            Console.Write("IsAuthenticated:" + User.Identity.IsAuthenticated);
            Console.Write("Name:" + User.Identity.Name);

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            else
            {
                throw new Exception("Bad Request... :(");
            }
        }
    }
}
