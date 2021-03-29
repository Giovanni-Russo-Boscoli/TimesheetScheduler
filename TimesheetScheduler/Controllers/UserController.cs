using System.Web;
using System.Web.Mvc;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Models;
using TimesheetScheduler.Services;

namespace TimesheetScheduler.Controllers
{
    public class UserController : Controller
    {
        //private string jsonUserServerPath = "~/JsonData/jsonUser.json";
        //private string jsonRatesAndRolesServerPath = "~/JsonData/ratesAndRoles.json";
        //private string jsonProjectIterationServerPath = "~/JsonData/projectIteration.json";

        private HttpServerUtility _server;
        private System.Web.SessionState.HttpSessionState _session;
        private static IReadJsonFiles _service;

        public UserController()
        {
            _server = System.Web.HttpContext.Current.Server;
            _session = System.Web.HttpContext.Current.Session;
            _service = new ReadJsonFiles();
        }

        public static IReadJsonFiles Current
        {
            get { return _service; }
        }


        //public UserController()
        //{
        //    _server = System.Web.HttpContext.Current.Server;
        //    _session = System.Web.HttpContext.Current.Session;
        //    this._service = new ReadJsonFiles();
        //}

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ReadJsonUserFile()
        {
            return Json(Current.DeserializeReadJsonUserFile(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ReadJsonRatesAndRolesFile()
        {
            return Json(Current.DeserializeReadJsonRatesAndRolesFile(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ReadJsonProjectIterationFile()
        {
            return Json(Current.DeserializeReadJsonProjectIterationFile(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SubmitUserButton(JsonUser jsonFile, string ButtonType) {
            return Json(Current.SubmitUserButton(jsonFile, ButtonType), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SubmitRoleButton(JsonRatesAndRoles jsonRoles, string ButtonType)
        {
            return Json(Current.SubmitRoleButton(jsonRoles, ButtonType), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SubmitTFSReferenceButton(JsonProjectIteration jsonTFS, string ButtonType)
        {
            return Json(Current.SubmitTFSReferenceButton(jsonTFS, ButtonType), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            //TODO INCLUDE ALLOWTODELETEUSER
            return Json(Current.DeleteUser(userId), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteRole(int roleId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS ROLE, IF YES PREVENT DELETION
            if (Current.allowToDeleteRole(roleId)) //TODO REMOVE THIS STATEMENT FROM HERE AND INCLUDE IN ReadJsonFile
            {
                return Json(Current.DeleteRole(roleId), JsonRequestBehavior.AllowGet);
            }
            return Json("There are users associated with this role.", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteTFSProject(int tfsProjectId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS PROJECT ASSOCIATED, IF YES PREVENT DELETION
            if (Current.allowToDeleteTFSProject(tfsProjectId))//TODO REMOVE THIS STATEMENT FROM HERE AND INCLUDE IN ReadJsonFile
            {
                return Json(Current.DeleteTFSProject(tfsProjectId), JsonRequestBehavior.AllowGet);
            }
            return Json("There are users associated with this TFS Project.", JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public bool IsUserLoggedAdmin()
        {
            return Current.IsUserLoggedAdmin();
        }

        [HttpGet]
        public string GetUserNameLogged()
        {
            return Current.GetUserNameLogged();
        }

        //[HttpGet]
        //public decimal GetMemberRate(string username) {
        //    var rates = ReadJsonRatesAndRolesFile();
        //    foreach (var item in rates)
        //    {
        //        item.
        //    }
            
        //}
    }

}