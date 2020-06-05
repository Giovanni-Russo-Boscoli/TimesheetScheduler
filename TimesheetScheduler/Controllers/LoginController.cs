using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Controllers
{
    [Authorize]
    public class LoginController : Controller
    {
        // GET: Login
        [AllowAnonymous]
        public ActionResult Index()
        {

            if (HttpContext.User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SignIn(LoginTimesheetScheduler model)//, string returnUrl)
        {

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (ModelState.IsValid)
            {
                var userExists = IsAuthenticated(model.UserName, model.Password);

                if (userExists)
                {
                    SaveUserSession(model.UserName);
                   
                    //Session["userId"] = userId;
                    //if (model.RememberMe)
                    //{
                    //    FormsAuthentication.Initialize();
                    //    DateTime expires = DateTime.Now.AddDays(30);

                    //    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, model.UserName, DateTime.Now,
                    //        expires, true, String.Empty, FormsAuthentication.FormsCookiePath);

                    //    string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                    //    HttpCookie authCookie = new HttpCookie("TimesheetScheduler_UserLogged", encryptedTicket)
                    //    {
                    //        Expires = expires
                    //    };

                    //    Response.Cookies.Add(authCookie);
                    //}

                    //FormsAuthentication.SetAuthCookie("Timesheet", model.RememberMe);                    
                    //return RedirectToAction("Index", "Home");
                    //if (!model.RememberMe)
                    //{
                    //    FormsAuthentication.RedirectFromLoginPage(model.UserName, false);
                    //}
                    if (model.RememberMe)
                    {
                        FormsAuthentication.Initialize();
                        DateTime expires = DateTime.Now.AddDays(20);
                        FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
                          model.UserName, 
                          DateTime.Now, 
                          expires, // value of time out property
                          true, // Value of IsPersistent property
                          String.Empty,
                          FormsAuthentication.FormsCookiePath);

                        string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                        HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket) {
                            Expires = expires
                        };

                        Response.Cookies.Add(authCookie);

                        //string returnUrl = FormsAuthentication.GetRedirectUrl(model.UserName, true);
                        //return RedirectToAction("Index", "Home");
                    }
                    FormsAuthentication.RedirectFromLoginPage(model.UserName, false); //redirect to 'defaultUrl' configured in web.config
                }
            }

            ModelState.AddModelError(string.Empty, "The username or password is incorrect");
            return View("Index", model);
        }

        [AllowAnonymous]
        public bool IsAuthenticated(string userName, string pwd)
        {
            //var domainName = "DESKTOP-A7IHSEN";
            var domainName = "welfare.irlgov.ie";
            var isValid = false;

            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName))
            //using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                isValid = pc.ValidateCredentials(userName, pwd);
            }

            return isValid;
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Index();
        }

        private void SaveUserSession(string userName) {
            DirectoryEntry de = new DirectoryEntry("WinNT://" + Environment.UserDomainName + "/" + userName);// Environment.UserName);
            Session["userLogged"] = de.Properties["FullName"].Value.ToString();
        }

        [AllowAnonymous]
        public string GetLocalUser()
        {
            //DirectoryEntry de = new DirectoryEntry("WinNT://" + Environment.UserDomainName + "/" + Environment.UserName);
            //return de.Properties["FullName"].Value.ToString().Replace(" ", "");
            return "";//suggest the user name
        }

        //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        //public sealed class MyAuthorizeAttribute : AuthorizeAttribute
        //{
        //    public override void OnAuthorization(AuthorizationContext filterContext)
        //    {
        //        base.OnAuthorization(filterContext);
        //        OnAuthorizationHelp(filterContext);
        //    }

        //    internal void OnAuthorizationHelp(AuthorizationContext filterContext)
        //    {

        //        if (filterContext.Result is HttpUnauthorizedResult)
        //        {
        //            if (filterContext.HttpContext.Request.IsAjaxRequest())
        //            {
        //                filterContext.HttpContext.Response.StatusCode = 401;
        //                filterContext.HttpContext.Response.End();
        //            }
        //        }
        //    }
        //}
    }
}