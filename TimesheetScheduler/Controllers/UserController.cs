using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        [HttpGet]
        public JsonResult ReadJsonVATFile()
        {
            return Json(Current.DeserializeReadJsonVATFile(), JsonRequestBehavior.AllowGet);
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
        public JsonResult SubmitTeamDivisionButton(TeamDivision jsonTeamDivision, string ButtonType)
        {
            int teamId;
            if (!int.TryParse(Request.Form["hiddenTeamId"], out teamId)) {
                throw new Exception("(SubmitTeamDivisionButton) -> Error when converting teamId value");
            }
            return Json(Current.SubmitTeamDivisionButton(jsonTeamDivision, teamId, ButtonType), JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public JsonResult DeleteTeamDivision(int teamDivisionId, int teamId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS PROJECT ASSOCIATED, IF YES PREVENT DELETION
            if (Current.allowToDeleteTeamDivision(teamDivisionId, teamId))
            {
                return Json(Current.DeleteTeamDivision(teamDivisionId, teamId), JsonRequestBehavior.AllowGet);
            }
            return Json("There are users associated with this Team Division.", JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public JsonResult SelectUsersByTeamDivision(int teamId) {

            var project = Current.DeserializeReadJsonProjectIterationFile().Where(x => x.Id == teamId).FirstOrDefault();

            var usersGroupedByTeamDivision = Current.DeserializeReadJsonUserFile().Where(x=>x.Active && x.ProjectId == project.Id).GroupBy(x=>x.TeamDivision);

            IList<UsersByTeamDivisionDTO> _formattedList = new List<UsersByTeamDivisionDTO>();

            foreach (var user in usersGroupedByTeamDivision)
            {
                _formattedList.Add(new UsersByTeamDivisionDTO()
                {
                    TeamDivision = user.Key,
                    TeamMembers = user.OrderBy(x=>x.Name).ToList()
                });
            }

            return Json(_formattedList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetTeamDivisionByTeamId(int teamId) {
            var teamDivision = Current.DeserializeReadJsonProjectIterationFile().Where(x => x.Id == teamId).FirstOrDefault();
            var result = (teamDivision != null ? teamDivision.TeamDivision : null);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //[HttpGet]
        //public JsonResult SelectTeams ()
        //{
        //    return Json(Current.DeserializeReadJsonProjectIterationFile(), JsonRequestBehavior.AllowGet);
        //}

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