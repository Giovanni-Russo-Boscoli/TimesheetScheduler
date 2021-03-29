﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
//using System.Web.Http.ModelBinding;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Services
{
    public class ReadJsonFiles : IReadJsonFiles
    {
        private string jsonUserServerPath = "~/JsonData/jsonUser.json";
        private string jsonRatesAndRolesServerPath = "~/JsonData/ratesAndRoles.json";
        private string jsonProjectIterationServerPath = "~/JsonData/projectIteration.json";

        private HttpServerUtility _server;
        private System.Web.SessionState.HttpSessionState _session;

        public ReadJsonFiles()
        {
            _server = System.Web.HttpContext.Current.Server;
            _session = System.Web.HttpContext.Current.Session;
        }

        public bool WriteJsonUserFile(IList<JsonUser> jsonFile)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(_server.MapPath(jsonUserServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonFile, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        public bool WriteJsonRolesFile(IList<JsonRatesAndRoles> jsonRole)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(_server.MapPath(jsonRatesAndRolesServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonRole, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        public bool WriteJsonTFSProjectFile(IList<JsonProjectIteration> jsonTFSProject)
        {
            var success = false;
            using (StreamWriter w = new StreamWriter(_server.MapPath(jsonProjectIterationServerPath)))
            {
                string jsonData = JsonConvert.SerializeObject(jsonTFSProject, Formatting.Indented);
                w.Write(jsonData);
                success = true;
            }
            return success;
        }

        public IList<JsonUser> DeserializeReadJsonUserFile()
        {
            using (StreamReader r = new StreamReader(_server.MapPath(jsonUserServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var result = jss.Deserialize<List<JsonUser>>(json);
                return result;
            }
        }

        public int ReturnNextId_Users()
        {
            var nextId = DeserializeReadJsonUserFile().Max(x => x.Id);
            return ++nextId;
        }

        public int ReturnNextId_Roles()
        {
            var nextId = DeserializeReadJsonRatesAndRolesFile().Max(x => x.Id);
            return ++nextId;
        }

        public int ReturnNextId_TFSProjects()
        {
            var nextId = DeserializeReadJsonProjectIterationFile().Max(x => x.Id);
            return ++nextId;
        }

        public IList<JsonRatesAndRoles> DeserializeReadJsonRatesAndRolesFile()
        {
            using (StreamReader r = new StreamReader(_server.MapPath(jsonRatesAndRolesServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                return jss.Deserialize<List<JsonRatesAndRoles>>(json);
            }
        }

        public decimal ReturnRateByRole(string role)
        {
            return DeserializeReadJsonRatesAndRolesFile().Where(x => x.Role == role).Select(y => y.Rate).FirstOrDefault();
        }

        public decimal GetMemberRate(string username) {
            var result = Math.Round((DeserializeReadJsonUserFile().Where(x => x.Name.Equals(username)).FirstOrDefault().Rate), 2);
            return result;
        }

        public IList<JsonProjectIteration> DeserializeReadJsonProjectIterationFile()
        {
            using (StreamReader r = new StreamReader(_server.MapPath(jsonProjectIterationServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                return jss.Deserialize<List<JsonProjectIteration>>(json);
            }
        }

        public string ReturnIterationPathByProjectName(string projectNameTFS)
        {
            return DeserializeReadJsonProjectIterationFile().Where(x => x.ProjectNameTFS == projectNameTFS).Select(y => y.IterationPathTFS).FirstOrDefault();
        }

        public bool AddNewUser(JsonUser jsonFile)
        {
            var Items = new List<JsonUser>();
            using (StreamReader r = new StreamReader(_server.MapPath(jsonUserServerPath)))
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

        public bool AddNewRole(JsonRatesAndRoles jsonRole)
        {
            try
            {
                var Items = new List<JsonRatesAndRoles>();
                using (StreamReader r = new StreamReader(_server.MapPath(jsonRatesAndRolesServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonRatesAndRoles>>(json);
                    jsonRole.Id = ReturnNextId_Roles();
                    jsonRole.Rate = jsonRole.Rate;
                    Items.Add(jsonRole);
                }

                return WriteJsonRolesFile(Items);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool AddNewTFSProject(JsonProjectIteration jsonTFS)
        {
            try
            {
                var Items = new List<JsonProjectIteration>();
                using (StreamReader r = new StreamReader(_server.MapPath(jsonProjectIterationServerPath)))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Items = jss.Deserialize<List<JsonProjectIteration>>(json);
                    jsonTFS.Id = ReturnNextId_TFSProjects();
                    jsonTFS.IterationPathTFS = jsonTFS.IterationPathTFS;
                    jsonTFS.ProjectNameTFS = jsonTFS.ProjectNameTFS;
                    Items.Add(jsonTFS);
                }

                return WriteJsonTFSProjectFile(Items);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public bool UpdateUser(JsonUser jsonFile)
        {
            var Items = new List<JsonUser>();
            using (StreamReader r = new StreamReader(_server.MapPath(jsonUserServerPath)))
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

        public bool UpdateRole(JsonRatesAndRoles jsonRole)
        {
            try
            {
                var Items = new List<JsonRatesAndRoles>();
                var roleName = "";
                using (StreamReader r = new StreamReader(_server.MapPath(jsonRatesAndRolesServerPath)))
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

                return WriteJsonRolesFile(Items);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool UpdateTFSProject(JsonProjectIteration jsonTFS)
        {
            try
            {
                var Items = new List<JsonProjectIteration>();
                var tfsProjectIterationName = "";
                using (StreamReader r = new StreamReader(_server.MapPath(jsonProjectIterationServerPath)))
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

                return WriteJsonTFSProjectFile(Items);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool SubmitRoleButton(JsonRatesAndRoles jsonRoles, string ButtonType)
        {
            bool result = false;
            switch (ButtonType)
            {
                case "CreateRole":
                    {
                        result = this.AddNewRole(jsonRoles);
                        break;
                    }
                case "UpdateRole":
                    {
                        result = this.UpdateRole(jsonRoles);
                        break;
                    }
            }
            return result;
        }

        public bool SubmitTFSReferenceButton(JsonProjectIteration jsonTFS, string ButtonType)
        {
            bool result = false;
            switch (ButtonType)
            {
                case "CreateTFSProject":
                    {
                        result = this.AddNewTFSProject(jsonTFS);
                        break;
                    }
                case "UpdateTFSProject":
                    {
                        result = this.UpdateTFSProject(jsonTFS);
                        break;
                    }
            }
            //return result;
            return result;
        }

        public bool SubmitUserButton(JsonUser jsonFile, string ButtonType)
        {
            var result = false;
            switch (ButtonType)
            {
                case "CreateUser":
                    {
                        result = this.AddNewUser(jsonFile);
                        break;
                    }
                case "UpdateUser":
                    {
                        result = this.UpdateUser(jsonFile);
                        break;
                    }
            }
            return result;
        }

        public bool DeleteUser(int userId)
        {
            var Items = new List<JsonUser>();
            using (StreamReader r = new StreamReader(_server.MapPath(jsonUserServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Items = jss.Deserialize<List<JsonUser>>(json);

                Items.RemoveAll(x => x.Id == userId);
            }

            return this.WriteJsonUserFile(Items);
        }

        public bool DeleteRole(int roleId)
        {
            var Items = new List<JsonRatesAndRoles>();
            using (StreamReader r = new StreamReader(_server.MapPath(jsonRatesAndRolesServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Items = jss.Deserialize<List<JsonRatesAndRoles>>(json);

                Items.RemoveAll(x => x.Id == roleId);
            }
            return this.WriteJsonRolesFile(Items);
        }

        public bool DeleteTFSProject(int tfsProjectId)
        {
            //CHECK IF THERE ARE ANY USERS WITH THIS PROJECT ASSOCIATED, IF YES PREVENT DELETION
            var Items = new List<JsonProjectIteration>();
            using (StreamReader r = new StreamReader(_server.MapPath(jsonProjectIterationServerPath)))
            {
                string json = r.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Items = jss.Deserialize<List<JsonProjectIteration>>(json);

                Items.RemoveAll(x => x.Id == tfsProjectId);
            }
            return this.WriteJsonTFSProjectFile(Items);
        }


        public void updateUsersRoles(string originalRole, string newRole, decimal rate)
        {
            var users = DeserializeReadJsonUserFile();
            users.Where(x => x.Role.Equals(originalRole)).ToList().ForEach(y => { y.Role = newRole; y.Rate = rate; });
            WriteJsonUserFile(users);
        }

        public void updateUsersTFSProject(string originalTFSProject, string newIterationPathTFS, string newTFSProject)
        {
            var users = DeserializeReadJsonUserFile();
            users.Where(x => x.IterationPathTFS.Equals(originalTFSProject)).ToList().ForEach(y => { y.IterationPathTFS = newIterationPathTFS; y.ProjectNameTFS = newTFSProject; });
            WriteJsonUserFile(users);
        }

        public bool allowToDeleteRole(int roleId)
        {
            var role = DeserializeReadJsonRatesAndRolesFile().Where(x => x.Id == roleId).FirstOrDefault();
            return DeserializeReadJsonUserFile().Where(x => x.Role == role.Role).Count() < 1;
        }

        public bool allowToDeleteTFSProject(int tfsProjectId)
        {
            var tfsProject = DeserializeReadJsonProjectIterationFile().Where(x => x.Id == tfsProjectId).FirstOrDefault();
            return DeserializeReadJsonUserFile().Where(x => x.IterationPathTFS == tfsProject.IterationPathTFS).Count() < 1;
        }

        public string modelStateErrors(ICollection<ModelState> modelStateValues)
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

        public bool IsUserLoggedAdmin()
        {
            var mySession = _session["userLoggedName"] as string;
            IList<JsonUser> _users = this.DeserializeReadJsonUserFile();
            return _users.Where(x => x.Name == mySession).FirstOrDefault().Access == "Admin";
        }

        public string GetUserNameLogged()
        {
            return _session["userLoggedName"] as string;
        }
    }
}