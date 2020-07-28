using System.Web;
using System.Web.Optimization;

namespace TimesheetScheduler
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*",
                        "~/Scripts/jquery.unobtrusive-ajax.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                    "~/Scripts/jquery-ui-1.12.1.js"));

            bundles.Add(new ScriptBundle("~/bundles/momentjs").Include(
                    "~/Scripts/moment.js"));

            bundles.Add(new ScriptBundle("~/bundles/fullcalendarjs").Include(
                        "~/Scripts/fullcalendar.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquerydateformat").Include(
                        "~/Scripts/jquery.dateFormat-1.0.js"));

            bundles.Add(new ScriptBundle("~/bundles/toastrjs").Include(
                        "~/Scripts/toastr.js"));
                
            bundles.Add(new ScriptBundle("~/bundles/util").Include(
                      "~/Scripts/Util.js"));

            bundles.Add(new ScriptBundle("~/bundles/timesheetscheduler").Include(
                      "~/Scripts/TimesheetSchedulerCalendar.js"));
                      //"~/Scripts/DataTables/jquery.dataTables.js"));

            bundles.Add(new ScriptBundle("~/bundles/login").Include(
                      "~/Scripts/Login.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/bootstrap-toggle.js"));

            bundles.Add(new ScriptBundle("~/bundles/mustachejs").Include(
                     "~/Scripts/Mustache.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquerymask").Include(
                     "~/Scripts/jquery.mask.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/fullcalendar.css",
                      "~/Content/toastr.css",
                      "~/Content/site.css",
                      "~/Content/bootstrap-toggle.css",
                      "~/css/font-awesome.css"));

            bundles.Add(new StyleBundle("~/Content/User").Include(
                     "~/Content/User.css"));

            bundles.Add(new StyleBundle("~/Content/timesheetscheduler").Include(
                      "~/Content/progressBarAnimation.css",
                      "~/Content/TimesheetScheduler.css",
                      "~/Content/DataTables/css/jquery.dataTables.css"));

            bundles.Add(new StyleBundle("~/Content/login").Include(
                      "~/Content/Login.css"));

            //BundleTable.EnableOptimizations = true;
        }
    }
}
