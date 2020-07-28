using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TimesheetScheduler.Controllers
{
    public class ExcelFileController : Controller
    {
        // GET: ExcelFile
        public ActionResult Index()
        {
            return View();
        }

        //move to this file all the methods responsible to create the excel timesheet file
    }
}