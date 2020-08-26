using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Controllers
{
    public class UserController : Controller
    {
        private string jsonUserServerPath = "~/JsonData/jsonUser.json";
        private string jsonRatesAndRolesServerPath = "~/JsonData/ratesAndRoles.json";
        private string jsonProjectIterationServerPath = "~/JsonData/projectIteration.json";

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        private bool WriteJsonUserFile(List<JsonUser> jsonFile)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(Server.MapPath(jsonUserServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonFile, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        private bool WriteJsonRolesFile(List<JsonRatesAndRoles> jsonRole)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(Server.MapPath(jsonRatesAndRolesServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonRole, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        private bool WriteJsonTFSProjectFile(List<JsonProjectIteration> jsonTFSProject)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(Server.MapPath(jsonProjectIterationServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonTFSProject, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        [HttpGet]
        public JsonResult ReadJsonUserFile()
        {
            return Json(DeserializeReadJsonUserFile(), JsonRequestBehavior.AllowGet);
        }

        private List<JsonUser> DeserializeReadJsonUserFile()
        {
            using (StreamReader r = new StreamReader(Server.MapPath(jsonUserServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var result = jss.Deserialize<List<JsonUser>>(json);
                return result;
            }
        }

        private int ReturnNextId_Users()
        {
            var nextId = DeserializeReadJsonUserFile().Max(x => x.Id);
            return ++nextId;
        }

        private int ReturnNextId_Roles()
        {
            var nextId = DeserializeReadJsonRatesAndRolesFile().Max(x => x.Id);
            return ++nextId;
        }

        private int ReturnNextId_TFSProjects()
        {
            var nextId = DeserializeReadJsonProjectIterationFile().Max(x => x.Id);
            return ++nextId;
        }

        [HttpGet]
        public JsonResult ReadJsonRatesAndRolesFile()
        {
            return Json(DeserializeReadJsonRatesAndRolesFile(), JsonRequestBehavior.AllowGet);
        }

        private List<JsonRatesAndRoles> DeserializeReadJsonRatesAndRolesFile()
        {
            using (StreamReader r = new StreamReader(Server.MapPath(jsonRatesAndRolesServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                return jss.Deserialize<List<JsonRatesAndRoles>>(json);
            }
        }

        private decimal ReturnRateByRole(string role)
        {
            return DeserializeReadJsonRatesAndRolesFile().Where(x => x.Role == role).Select(y => y.Rate).FirstOrDefault();
        }

        [HttpGet]
        public JsonResult ReadJsonProjectIterationFile()
        {
            return Json(DeserializeReadJsonProjectIterationFile(), JsonRequestBehavior.AllowGet);
        }

        private List<JsonProjectIteration> DeserializeReadJsonProjectIterationFile()
        {
            using (StreamReader r = new StreamReader(Server.MapPath(jsonProjectIterationServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                return jss.Deserialize<List<JsonProjectIteration>>(json);
            }
        }

        private string ReturnIterationPathByProjectName(string projectNameTFS)
        {
            return DeserializeReadJsonProjectIterationFile().Where(x => x.ProjectNameTFS == projectNameTFS).Select(y => y.IterationPathTFS).FirstOrDefault();
        }

        [HttpPost]
        public bool SubmitUserButton(JsonUser jsonFile, string ButtonType) {
            var result = false;
            switch (ButtonType)
            {
                case "CreateUser":
                    {
                        result = AddNewUser(jsonFile);
                        break;
                    }
                case "UpdateUser":
                    {
                        result = UpdateUser(jsonFile);
                        break;
                    }
            }
            return result;
        }

        [HttpPost]
        public JsonResult SubmitRoleButton(JsonRatesAndRoles jsonRoles, string ButtonType)
        {
            JsonResult result = new JsonResult();
            switch (ButtonType)
            {
                case "CreateRole":
                    {
                        result = AddNewRole(jsonRoles);
                        break;
                    }
                case "UpdateRole":
                    {
                        result = UpdateRole(jsonRoles);
                        break;
                    }
            }
            return result;
        }

        [HttpPost]
        public JsonResult SubmitTFSReferenceButton(JsonProjectIteration jsonTFS, string ButtonType)
        {
            JsonResult result = new JsonResult();
            switch (ButtonType)
            {
                case "CreateTFSProject":
                    {
                        result = AddNewTFSProject(jsonTFS);
                        break;
                    }
                case "UpdateTFSProject":
                    {
                        result = UpdateTFSProject(jsonTFS);
                        break;
                    }
            }
            return result;
        }


        private bool AddNewUser(JsonUser jsonFile)
        {
            if (ModelState.IsValid)
            {
                var Items = new List<JsonUser>();
                using (StreamReader r = new StreamReader(Server.MapPath(jsonUserServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonUser>>(json);

                    jsonFile.Rate = ReturnRateByRole(jsonFile.Role);
                    jsonFile.IterationPathTFS = ReturnIterationPathByProjectName(jsonFile.ProjectNameTFS);
                    jsonFile.Id = ReturnNextId_Users();

                    Items.Add(jsonFile);
                }

                return WriteJsonUserFile(Items);

            }
            return false;//TODO: FIX - IF USERS PAGE IS REFRESH AFTER ADDING A NEW USER IT HOLD THE OBJ AND SEND IT AGAIN TO BE SAVED - SHOULD REDIRECT TO Users AND NOT TO AddNewUser
        }

        private JsonResult AddNewRole(JsonRatesAndRoles jsonRole)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var Items = new List<JsonRatesAndRoles>();
                    using (StreamReader r = new StreamReader(Server.MapPath(jsonRatesAndRolesServerPath)))
                    {
                        string json = r.ReadToEnd();
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        Items = jss.Deserialize<List<JsonRatesAndRoles>>(json);
                        jsonRole.Id = ReturnNextId_Roles();
                        jsonRole.Rate = jsonRole.Rate;
                        Items.Add(jsonRole);
                    }

                    return Json(WriteJsonRolesFile(Items), JsonRequestBehavior.AllowGet);

                }
                return Json(modelStateErrors(ModelState.Values), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        private JsonResult AddNewTFSProject(JsonProjectIteration jsonTFS)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var Items = new List<JsonProjectIteration>();
                    using (StreamReader r = new StreamReader(Server.MapPath(jsonProjectIterationServerPath)))
                    {
                        string json = r.ReadToEnd();
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        Items = jss.Deserialize<List<JsonProjectIteration>>(json);
                        jsonTFS.Id = ReturnNextId_TFSProjects();
                        jsonTFS.IterationPathTFS = jsonTFS.IterationPathTFS;
                        jsonTFS.ProjectNameTFS = jsonTFS.ProjectNameTFS;
                        Items.Add(jsonTFS);
                    }

                    return Json(WriteJsonTFSProjectFile(Items), JsonRequestBehavior.AllowGet);

                }
                return Json(modelStateErrors(ModelState.Values), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private bool UpdateUser(JsonUser jsonFile)
        {
            if (ModelState.IsValid)
            {
                var Items = new List<JsonUser>();
                using (StreamReader r = new StreamReader(Server.MapPath(jsonUserServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonUser>>(json);

                    var item = Items.Where(x => x.Id == jsonFile.Id).FirstOrDefault();
                    
                    item.Active = jsonFile.Active;
                    item.Name = jsonFile.Name;
                    item.Email = jsonFile.Email;
                    item.Role = jsonFile.Role;
                    item.Category = jsonFile.Category;
                    item.Chargeable = jsonFile.Chargeable;
                    item.ProjectNameTFS = jsonFile.ProjectNameTFS;
                    item.Access = jsonFile.Access;
                    item.Rate = ReturnRateByRole(jsonFile.Role);
                    item.IterationPathTFS = ReturnIterationPathByProjectName(jsonFile.ProjectNameTFS);

                    Items[Items.FindIndex(ind => ind.Id == jsonFile.Id)] = item;
                }

                return WriteJsonUserFile(Items);

            }
            return false;//TODO: FIX - IF USERS PAGE IS REFRESH AFTER ADDING A NEW USER IT HOLD THE OBJ AND SEND IT AGAIN TO BE SAVED - SHOULD REDIRECT TO Users AND NOT TO AddNewUser
        }

        private JsonResult UpdateRole(JsonRatesAndRoles jsonRole)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var Items = new List<JsonRatesAndRoles>();
                    var roleName = "";
                    using (StreamReader r = new StreamReader(Server.MapPath(jsonRatesAndRolesServerPath)))
                    {
                        string json = r.ReadToEnd();
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        Items = jss.Deserialize<List<JsonRatesAndRoles>>(json);

                        var item = Items.Where(x => x.Id == jsonRole.Id).FirstOrDefault();
                        roleName = item.Role;//delete when using db
                        item.Role = jsonRole.Role;
                        item.Rate = jsonRole.Rate;
                        item.ShortName = jsonRole.ShortName;
                        Items[Items.FindIndex(ind => ind.Id == jsonRole.Id)] = item;
                    }

                    //UPDATE USERS WITH THIS ROLE
                    updateUsersRoles(roleName, jsonRole.Role, jsonRole.Rate);

                    return Json(WriteJsonRolesFile(Items), JsonRequestBehavior.AllowGet);

                }
                return Json(modelStateErrors(ModelState.Values), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        private JsonResult UpdateTFSProject(JsonProjectIteration jsonTFS)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var Items = new List<JsonProjectIteration>();
                    var tfsProjectIterationName = "";
                    using (StreamReader r = new StreamReader(Server.MapPath(jsonProjectIterationServerPath)))
                    {
                        string json = r.ReadToEnd();
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        Items = jss.Deserialize<List<JsonProjectIteration>>(json);

                        var item = Items.Where(x => x.Id == jsonTFS.Id).FirstOrDefault();
                        tfsProjectIterationName = item.IterationPathTFS;//delete when using db
                        item.ProjectNameTFS = jsonTFS.ProjectNameTFS;
                        item.IterationPathTFS = jsonTFS.IterationPathTFS;
                        Items[Items.FindIndex(ind => ind.Id == jsonTFS.Id)] = item;
                    }

                    //UPDATE USERS WITH THIS ROLE
                    updateUsersTFSProject(tfsProjectIterationName, jsonTFS.IterationPathTFS, jsonTFS.ProjectNameTFS);

                    return Json(WriteJsonTFSProjectFile(Items), JsonRequestBehavior.AllowGet);

                }
                return Json(modelStateErrors(ModelState.Values), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void updateUsersRoles(string originalRole, string newRole, decimal rate)
        {
            var users = DeserializeReadJsonUserFile();
            users.Where(x => x.Role.Equals(originalRole)).ToList().ForEach(y => { y.Role = newRole; y.Rate = rate; });
            WriteJsonUserFile(users);
        }

        private void updateUsersTFSProject(string originalTFSProject, string newIterationPathTFS, string newTFSProject)
        {
            var users = DeserializeReadJsonUserFile();
            users.Where(x => x.IterationPathTFS.Equals(originalTFSProject)).ToList().ForEach(y => { y.IterationPathTFS = newIterationPathTFS; y.ProjectNameTFS = newTFSProject; });
            WriteJsonUserFile(users);
        }

        [HttpPost]
        public bool DeleteUser(int userId)
        {
            var Items = new List<JsonUser>();
            using (StreamReader r = new StreamReader(Server.MapPath(jsonUserServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Items = jss.Deserialize<List<JsonUser>>(json);

                Items.RemoveAll(x => x.Id == userId);
            }

            return WriteJsonUserFile(Items);

        }

        [HttpPost]
        public JsonResult DeleteRole(int roleId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS ROLE, IF YES PREVENT DELETION
            if (allowToDeleteRole(roleId))
            {
                var Items = new List<JsonRatesAndRoles>();
                using (StreamReader r = new StreamReader(Server.MapPath(jsonRatesAndRolesServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonRatesAndRoles>>(json);

                    Items.RemoveAll(x => x.Id == roleId);
                }
                return Json(WriteJsonRolesFile(Items), JsonRequestBehavior.AllowGet);
            }
            return Json("There are users associated with this role.", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteTFSProject(int tfsProjectId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS PROJECT ASSOCIATED, IF YES PREVENT DELETION
            if (allowToDeleteTFSProject(tfsProjectId))
            {
                var Items = new List<JsonProjectIteration>();
                using (StreamReader r = new StreamReader(Server.MapPath(jsonProjectIterationServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonProjectIteration>>(json);

                    Items.RemoveAll(x => x.Id == tfsProjectId);
                }
                return Json(WriteJsonTFSProjectFile(Items), JsonRequestBehavior.AllowGet);
            }
            return Json("There are users associated with this TFS Project.", JsonRequestBehavior.AllowGet);
        }

        private bool allowToDeleteRole(int roleId)
        {
            var role = DeserializeReadJsonRatesAndRolesFile().Where(x => x.Id == roleId).FirstOrDefault();
            return DeserializeReadJsonUserFile().Where(x => x.Role == role.Role).Count() < 1;
        }

        private bool allowToDeleteTFSProject(int tfsProjectId)
        {
            var tfsProject = DeserializeReadJsonProjectIterationFile().Where(x => x.Id == tfsProjectId).FirstOrDefault();
            return DeserializeReadJsonUserFile().Where(x => x.IterationPathTFS == tfsProject.IterationPathTFS).Count() < 1;
        }

        private string modelStateErrors(ICollection<ModelState> modelStateValues)
        {
            StringBuilder strErrors = new StringBuilder();
            foreach (ModelState modelState in modelStateValues) 
            {
                foreach (ModelError error in modelState.Errors)
                {
                    strErrors.Append(error.ErrorMessage + "\n");
                }
            }
            return strErrors.ToString();
        }

    }

}