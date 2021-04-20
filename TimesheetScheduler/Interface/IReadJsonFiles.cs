using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.Mvc;
using TimesheetScheduler.Models;

namespace TimesheetScheduler.Interface
{
    public interface IReadJsonFiles
    {
        bool WriteJsonUserFile(IList<JsonUser> jsonFile);

        bool WriteJsonRolesFile(IList<JsonRatesAndRoles> jsonRole);

        bool WriteJsonTFSProjectFile(IList<JsonProjectIteration> jsonTFSProject);

        IList<JsonUser> DeserializeReadJsonUserFile();

        int ReturnNextId_Users();

        int ReturnNextId_Roles();

        int ReturnNextId_TFSProjects();

        decimal GetMemberRate(string username);

        IList<JsonRatesAndRoles> DeserializeReadJsonRatesAndRolesFile();

        decimal ReturnRateByRole(string role);

        IList<JsonProjectIteration> DeserializeReadJsonProjectIterationFile();

        string ReturnIterationPathByProjectName(string projectNameTFS);
        bool AddNewUser(JsonUser jsonFile);

        bool AddNewRole(JsonRatesAndRoles jsonRole);

        bool AddNewTFSProject(JsonProjectIteration jsonTFS);
        
        bool UpdateUser(JsonUser jsonFile);

        bool UpdateRole(JsonRatesAndRoles jsonRole);

        bool UpdateTFSProject(JsonProjectIteration jsonTFS);

        bool SubmitRoleButton(JsonRatesAndRoles jsonRoles, string ButtonType);

        bool SubmitTFSReferenceButton(JsonProjectIteration jsonTFS, string ButtonType);

        bool SubmitUserButton(JsonUser jsonFile, string ButtonType);

        bool DeleteUser(int userId);

        bool DeleteRole(int roleId);

        bool DeleteTFSProject(int tfsProjectId);

        void updateUsersRoles(string originalRole, string newRole, decimal rate);

        void updateUsersTFSProject(string originalTFSProject, string newIterationPathTFS, string newTFSProject);

        bool allowToDeleteRole(int roleId);

        bool allowToDeleteTFSProject(int tfsProjectId);

        string modelStateErrors(ICollection<ModelState> modelStateValues);

        bool IsUserLoggedAdmin();

        string GetUserNameLogged();

        string GetMemberTeamDivision(string username);
    }
}
