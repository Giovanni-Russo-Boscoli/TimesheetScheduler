using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http.Filters;
using System.Web.Mvc;
using System.Web.Security;
using ActionFilterAttribute = System.Web.Mvc.ActionFilterAttribute;

namespace TimesheetScheduler.Models
{
    public class SessionExpireFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;

            string value = HttpContext.Current.Session["userLogged"] as string;
            if (string.IsNullOrEmpty(value))
            {
                FormsAuthentication.SignOut();
                System.Web.HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
                filterContext.Result = new RedirectResult("~/Login/Index");
            }
            base.OnActionExecuting(filterContext);
        }
    }
}