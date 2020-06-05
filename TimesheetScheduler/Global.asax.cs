using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TimesheetScheduler
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_EndRequest()
        {
            var context = new HttpContextWrapper(Context);

            //Do a direct 401 unautorized
            if (Context.Response.StatusCode == 302 && context.Request.IsAjaxRequest())
            {
                Context.Response.Clear();
                Context.Response.StatusCode = 401;
            }
        }

    }
}
