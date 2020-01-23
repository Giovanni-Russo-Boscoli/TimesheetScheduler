using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Net;
using System.Web.Mvc;

namespace TimesheetScheduler.Controllers
{
    public class AuthenticationController : Controller
    {
        // GET: Authentication
        [AllowAnonymous]
        public ActionResult Index()
        {
            //IList<string> list = new List<string>(){ "Giovanni Boscoli", "Renan Camara", "Amy Kelly" };


            ////check if user is registered in the database
            //if(list.Where( w => w.Equals(GetUserName())).Count() > 0)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            return View("Login");
        }

        //[HttpGet]
        [AllowAnonymous]
        public ActionResult Authenticate()//string user, string password)
        {
            var a = System.Web.HttpContext.Current.User.Identity;
            Uri tfsUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/");
            //NetworkCredential networkCredentials = new NetworkCredential(user, password);
            NetworkCredential networkCredentials = new NetworkCredential("GiovanniBoscoli@welfare.irlgov.ie", "?bCh+*p#d8MQ12");
            //NetworkCredential networkCredentials = new NetworkCredential("renancamara@welfare.irlgov.ie", "1998Senha");
            Microsoft.VisualStudio.Services.Common.WindowsCredential windowsCredentials = new Microsoft.VisualStudio.Services.Common.WindowsCredential(networkCredentials);
            VssCredentials basicCredentials = new VssCredentials(windowsCredentials);
            TfsTeamProjectCollection tfsColl = new TfsTeamProjectCollection(tfsUri, basicCredentials);

            //var aut = false;
            tfsColl.Authenticate();
            //aut = tfsColl.HasAuthenticated;
            //return tfsColl.HasAuthenticated;
            //return tfsColl.AuthorizedIdentity.DisplayName;
            //if (aut)
            //{
            //    //RedirectToAction("Index", "Home");
            //    RedirectAction();
            //}
            ////return aut;
            ////return RedirectToAction("Index", "Home");
            return RedirectToAction("Index", "Home");
            //return Redirect("Home/Index");
        }

        public ActionResult RedirectToHome()
        {
            return RedirectToAction("Index", "Home");
        }

        //private string GetUserName()
        //{
        //    return string.IsNullOrEmpty(UserPrincipal.Current.DisplayName) ? FormatDomainUserName(GetDomainUserName()) : UserPrincipal.Current.DisplayName;
        //}

        //private string FormatDomainUserName(string domainUserName)
        //{
        //    return domainUserName.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
        //}

        //private string GetDomainUserName()
        //{
        //    return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        //}
    }
}