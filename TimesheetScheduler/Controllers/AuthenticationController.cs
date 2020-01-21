using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TimesheetScheduler.Controllers
{
    public class AuthenticationController : Controller
    {
        // GET: Authentication
        public ActionResult Index()
        {
            IList<string> list = new List<string>(){ "Giovanni Boscoli", "Renan Camara", "Amy Kelly" };


            //check if user is registered in the database
            if(list.Where( w => w.Equals(GetUserName())).Count() > 0)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        private string GetUserName()
        {
            return string.IsNullOrEmpty(UserPrincipal.Current.DisplayName) ? FormatDomainUserName(GetDomainUserName()) : UserPrincipal.Current.DisplayName;
        }

        private string FormatDomainUserName(string domainUserName)
        {
            return domainUserName.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
        }

        private string GetDomainUserName()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }
    }
}