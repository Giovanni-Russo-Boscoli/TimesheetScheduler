using Microsoft.Office.Interop.Excel;
using Microsoft.TeamFoundation.Client;
//
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Models;
using TimesheetScheduler.Services;
using TimesheetScheduler.ViewModel;
using Application = Microsoft.Office.Interop.Excel.Application;
//
using ProjectInfo = Microsoft.TeamFoundation.Server.ProjectInfo;

namespace TimesheetScheduler.Controllers
{
    [Authorize]
    [SessionExpireFilter]
    public class HomeController : Controller
    {

        public HomeController()
        {
            _utilService = new UtilService();
            requiredHours = _utilService.FetchRequiredHours();
        }

        private static IUtilService _utilService;

        private string jsonHolidaysServerPath = "~/JsonData/Holidays/jsonHolidays";//.json";

        private double requiredHours { get; set; }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SampleTest()
        {
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult StepsReports()
        {
            ViewBag.Message = "Tracking Process.";

            return View();
        }

        public string GetUserName()
        {
            return string.IsNullOrEmpty(UserPrincipal.Current.DisplayName) ? FormatDomainUserName(GetDomainUserName()) : UserPrincipal.Current.DisplayName;
        }

        public string GetDomainUserName()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }

        public string FormatDomainUserName(string domainUserName)
        {
            return domainUserName.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
        }

        #region TFS

        private static string GetUrlTfs()
        {
            return ConfigurationManager.AppSettings["urlTFS"].ToString();
        }

        private static string GetProjectNameTFS()
        {
            return ConfigurationManager.AppSettings["projectNameTFS"].ToString();
        }

        private static string GetIterationPathTFS()
        {
            return ConfigurationManager.AppSettings["iterationPathTFS"].ToString();
        }

        //public JsonResult ConnectTFS(string userName, int _month, int _year)
        //{
        //    IList<IList<WorkItemSerialized>> joinWorkItemsList = new List<IList<WorkItemSerialized>>();
        //    userName = userName.Replace("'", "''");
        //    joinWorkItemsList.Add(ReturnTFSEvents_ListWorkItems(userName, _month, _year));
        //    joinWorkItemsList.Add(ReturnTFSEvents_ListWorkItemsWithoutStartDate(userName));

        //    return Json(joinWorkItemsList, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost]
        public JsonResult ConnectTFS(UserDataSearchTFS userData)//, int _month, int _year)
        {
            //var _userController = new UserController();
            //var _isUserLoggedAdmin = _userController.IsUserLoggedAdmin();
            //var _userLoggedName = _userController.GetUserNameLogged();
            //if (_isUserLoggedAdmin || userData.UserName.Equals(_userLoggedName))
            // {
            IList<object> joinWorkitems = new List<object>();
            userData.UserName = userData.UserName.Replace("'", "''");
            joinWorkitems.Add(ReturnTFSEvents_ListWorkItems(userData));
            joinWorkitems.Add(ReturnTFSEvents_ListWorkItemsWithoutStartDate(userData));
            //FormatTasksForEmailConfirmation(userData);
            return Json(joinWorkitems, JsonRequestBehavior.AllowGet);
            //}
            //else
            //{
            //    throw new Exception("Only admin user can see other members' timesheet data");
            // }
        }

        //public void printMemberList()
        //{
        //    List<string> users = new List<string>();
        //    string teamProject = "BOM_MOD24";
        //    var _uri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/");
        //    TfsTeamProjectCollection tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(_uri);
        //    tpc.EnsureAuthenticated();
        //    var vcs = tpc.GetService<VersionControlServer>();

        //    Microsoft.TeamFoundation.VersionControl.Client.TeamProject tp = vcs.GetTeamProject(teamProject);
        //    TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(_uri);
        //    IGroupSecurityService sec = (IGroupSecurityService)projCollection.GetService(typeof(IGroupSecurityService));
        //    Identity[] appGroups = sec.ListApplicationGroups(tp.ArtifactUri.AbsoluteUri);
        //    var membersBOM = appGroups.Where(x => x.AccountName.Contains("BOM_MOD24"));
        //    foreach (Identity group in membersBOM)//appGroups)
        //    {
        //        Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
        //        foreach (Identity member in groupMembers)
        //        {
        //            //Console.WriteLine(member.DisplayName);
        //            if (member.Members != null)
        //            {
        //                foreach (string memberSid in member.Members)
        //                {
        //                    Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.None);
        //                    users.Add(memberInfo.DisplayName);
        //                }
        //            }
        //        }
        //    }
        //    users = users.OrderBy(x => x).Distinct().ToList();
        //}

        [HttpGet]
        public JsonResult ListUserByProject(string projectName)
        {

            if (string.IsNullOrEmpty(projectName))
            {
                projectName = GetProjectNameTFS();
            }

            var tfsUri = new Uri(GetUrlTfs());
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);

            ICommonStructureService Iss = (ICommonStructureService)projCollection.GetService(typeof(ICommonStructureService));
            ProjectInfo[] ProjInfo = Iss.ListProjects();
            var bomi_24 = ProjInfo.Where(x => x.Name.Equals(projectName)).FirstOrDefault().Uri;

            TfsTeamService teamService = projCollection.GetService<TfsTeamService>();
            TeamFoundationTeam defaultTeam = teamService.GetDefaultTeam(bomi_24, null);
            var allMembersBomi = defaultTeam.GetMembers(projCollection, MembershipQuery.Expanded).Where(m => !m.IsContainer);
            var names = allMembersBomi.Select(x => x.DisplayName).OrderBy(x => x).Distinct();

            return Json(names, JsonRequestBehavior.AllowGet);
        }

        ///DELETE
        public IList<WorkItemSerialized> ReturnTFSEvents_ListWorkItems_DELETE(UserDataSearchTFS userData)
        {
            try
            {
                IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();

                var _urlTFS = GetUrlTfs();
                Uri tfsUri = new Uri(_urlTFS);
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                //-------------------------
                //NetworkCredential networkCredentials = new NetworkCredential(@"welfare\RenanCamara", @"5000@Senha");
                //Microsoft.VisualStudio.Services.Common.WindowsCredential windowsCredentials = new Microsoft.VisualStudio.Services.Common.WindowsCredential(networkCredentials);
                //VssCredentials basicCredentials = new VssCredentials(windowsCredentials);
                //TfsTeamProjectCollection tfsColl = new TfsTeamProjectCollection(tfsUri,basicCredentials);
                //WorkItemStore WIS = (WorkItemStore)tfsColl.GetService(typeof(WorkItemStore));
                //tfsColl.Authenticate(); // make sure it is authenticate

                //-------------------------

                WorkItemCollection WIC = WIS.Query(
                    " SELECT [System.Id], " +
                    " [System.WorkItemType], " +
                    " [System.State], " +
                    " [System.AssignedTo], " +
                    " [System.Title], " +
                    " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                    " [Microsoft.VSTS.Scheduling.StartDate] " +
                    " FROM WorkItems " +
                    " WHERE [System.TeamProject] = '" + userData.ProjectNameTFS + "'" +
                    " AND [Iteration Path] = '" + userData.IterationPathTFS + "'" +
                    " AND [Assigned To] = '" + userData.UserName + "'" +
                    " AND [Work Item Type] = 'Task'" +  //only Task -> Test Case doesn't have the same fields, causing query errors
                    " ORDER BY [System.Id], [System.WorkItemType]");

                foreach (WorkItem wi in WIC)
                {
                    if (wi["Start Date"] != null)
                    {
                        DateTime _startDate = (DateTime)wi["Start Date"];
                        if (_startDate.Month == (userData.Month == 0 ? DateTime.Now.Month : userData.Month)
                            && _startDate.Year == (userData.Year == 0 ? DateTime.Now.Year : userData.Year))
                        {
                            var _workItemsLinked = "";
                            for (int i = 0; i < wi.WorkItemLinks.Count; i++)
                            {
                                _workItemsLinked += "#" + wi.WorkItemLinks[i].TargetId + " ";
                            }

                            listWorkItems.Add(new WorkItemSerialized()
                            {
                                Id = wi["Id"].ToString(),
                                Title = wi["Title"].ToString(),
                                StartDate = wi["Start Date"] != null ? (DateTime)wi["Start Date"] : (DateTime?)null,
                                Description = WebUtility.HtmlDecode(wi["Description"].ToString()),
                                CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : (double?)null,
                                RemainingWork = wi["Remaining Work"] != null ? (double)wi["Remaining Work"] : (double?)null,
                                WorkItemsLinked = _workItemsLinked,
                                State = wi.State,
                                LinkUrl = _urlTFS + userData.ProjectNameTFS + "/_queries?id=" + wi["Id"].ToString(),
                                IsWeekend = wi["Start Date"] != null ? IsWeekend((DateTime)wi["Start Date"]) : (bool?)null
                            });
                        }
                    }
                }
                return listWorkItems.OrderBy(x => x.StartDate).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public ConsolidatedMonthUserData ReturnTFSEvents_ListWorkItems(UserDataSearchTFS userData)
        {
            try
            {
                IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();

                var _urlTFS = GetUrlTfs();
               
                foreach (WorkItem wi in TFSQuery(userData))
                {
                    if (wi["Start Date"] != null)
                    {
                        DateTime _startDate = (DateTime)wi["Start Date"];
                        if (_startDate.Month == (userData.Month == 0 ? DateTime.Now.Month : userData.Month)
                            && _startDate.Year == (userData.Year == 0 ? DateTime.Now.Year : userData.Year))
                        {
                            var _workItemsLinked = "";
                            for (int i = 0; i < wi.WorkItemLinks.Count; i++)
                            {
                                _workItemsLinked += "#" + wi.WorkItemLinks[i].TargetId + " ";
                            }

                            var _item = new WorkItemSerialized()
                            {
                                Id = wi["Id"].ToString(),
                                Title = wi["Title"].ToString(),
                                StartDate = wi["Start Date"] != null ? (DateTime)wi["Start Date"] : (DateTime?)null,
                                Description = WebUtility.HtmlDecode(wi["Description"].ToString()),
                                WorkItemsLinked = _workItemsLinked,
                                State = wi.State,
                                LinkUrl = _urlTFS + userData.ProjectNameTFS + "/_queries?id=" + wi["Id"].ToString(),
                                IsWeekend = wi["Start Date"] != null ? IsWeekend((DateTime)wi["Start Date"]) : (bool?)null,
                                CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : 0,
                                RemainingWork = wi["Remaining Work"] != null ? (double)wi["Remaining Work"] : 0
                        };

                            listWorkItems.Add(_item);

                        }
                    }
                }

                return formatTFSResult(ref listWorkItems, userData.UserName);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public IList<WorkItemSerialized> ReturnTFSEvents_ListWorkItemsWithoutStartDate(UserDataSearchTFS userData)
        {
            IList<WorkItemSerialized> listWorkItemsWithoutStartDate = new List<WorkItemSerialized>();

            foreach (WorkItem wi in TFSQuery(userData))
            {
                if (wi["Start Date"] == null)
                {
                    var _workItemsLinked = "";
                    for (int i = 0; i < wi.WorkItemLinks.Count; i++)
                    {
                        _workItemsLinked += "#" + wi.WorkItemLinks[i].TargetId + " ";
                    }

                    listWorkItemsWithoutStartDate.Add(new WorkItemSerialized()
                    {
                        Id = wi["Id"].ToString(),
                        Title = wi["Title"].ToString(),
                        Description = wi["Description"].ToString(),
                        CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : (double?)null,
                        WorkItemsLinked = _workItemsLinked,
                        State = wi.State,
                        CreationDate = wi.CreatedDate
                    });

                }
            }

            return listWorkItemsWithoutStartDate.OrderByDescending(x => x.CreationDate).ToList();

        }

        private ConsolidatedMonthUserData formatTFSResult(ref IList<WorkItemSerialized> listWorkItems, string username)
        {
            try
            {
                ConsolidatedMonthUserData consolidatedMonthUserData = new ConsolidatedMonthUserData();
                consolidatedMonthUserData.ListWorkItem = listWorkItems.OrderBy(x => x.StartDate).ToList();

                double _chargeableHours;
                double _nonChargeableHours;
                foreach (var group in listWorkItems.GroupBy(x => x.StartDate))
                {

                    double _chargeableHoursPerDay;
                    double _nonChargeableHoursPerDay;
                    double _completedHours;
                    double _remainingWork;
                    var _totalChargerableHoursPerDay = requiredHours;

                    foreach (var item in group)
                    {
                        _chargeableHoursPerDay = 0;
                        _nonChargeableHoursPerDay = 0;
                        _completedHours = item.CompletedHours.HasValue ? item.CompletedHours.Value : 0;
                        _remainingWork = item.RemainingWork.HasValue ? item.RemainingWork.Value : 0;

                        if (item.IsWeekend.Value)
                        {
                            _chargeableHoursPerDay = 0;
                            _nonChargeableHoursPerDay = _completedHours + _remainingWork;
                        }
                        else
                        {
                            if (_totalChargerableHoursPerDay > 0)
                            {
                                if (_completedHours <= _totalChargerableHoursPerDay)
                                {
                                    _chargeableHoursPerDay = _completedHours;
                                    _nonChargeableHoursPerDay = _remainingWork;
                                    _totalChargerableHoursPerDay -= _completedHours;
                                }
                                else
                                {
                                    _chargeableHoursPerDay = _totalChargerableHoursPerDay;
                                    _nonChargeableHoursPerDay = (_completedHours - _totalChargerableHoursPerDay) + _remainingWork;
                                    _totalChargerableHoursPerDay = 0;
                                }
                            }
                            else
                            {
                                //has reached the limit of required hours per day
                                _chargeableHoursPerDay = 0;
                                _nonChargeableHoursPerDay = _completedHours + _remainingWork;
                            }
                        }
                        item.CompletedHours = _chargeableHoursPerDay;
                        item.RemainingWork = _nonChargeableHoursPerDay;

                    }

                    _chargeableHours = _nonChargeableHours = 0d;

                    if (group.FirstOrDefault().IsWeekend.Value)
                    {
                        _nonChargeableHours += group.Sum(x => x.CompletedHours.HasValue ? x.CompletedHours.Value : 0) + group.Sum(x => x.RemainingWork.HasValue ? x.RemainingWork.Value : 0);
                        consolidatedMonthUserData.NonChargeableHours += _nonChargeableHours;
                    }
                    else
                    {

                        _chargeableHours = group.Sum(x => x.CompletedHours.HasValue ? x.CompletedHours.Value : 0);
                        _nonChargeableHours = group.Sum(x => x.RemainingWork.HasValue ? x.RemainingWork.Value : 0);

                        if (_chargeableHours > requiredHours)
                        {
                            consolidatedMonthUserData.ChargeableHours += requiredHours;
                            consolidatedMonthUserData.NonChargeableHours += ((_chargeableHours - requiredHours) + _nonChargeableHours);
                        }
                        else
                        {
                            consolidatedMonthUserData.ChargeableHours += _chargeableHours;
                            consolidatedMonthUserData.NonChargeableHours += _nonChargeableHours;
                        }
                    }

                }

                consolidatedMonthUserData.UserName = username;
                consolidatedMonthUserData.TotalExcludingVAT = consolidatedMonthUserData.RateExcludingVAT * (decimal)consolidatedMonthUserData.WorkedDays;
                consolidatedMonthUserData.TotalIncludingVAT = consolidatedMonthUserData.TotalExcludingVAT + (consolidatedMonthUserData.TotalExcludingVAT * (_utilService.FetchVat() / 100));

                return consolidatedMonthUserData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private WorkItemCollection TFSQuery(UserDataSearchTFS userData) {
            try
            {
                Uri tfsUri = new Uri(GetUrlTfs());
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                var projectName = GetProjectNameTFS();
                var _iterationPath = GetIterationPathTFS();

                WorkItemCollection WIC = WIS.Query(
                    " SELECT [System.Id], " +
                    " [System.WorkItemType], " +
                    " [System.State], " +
                    " [System.AssignedTo], " +
                    " [System.Title], " +
                    " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                    " [Microsoft.VSTS.Scheduling.StartDate] " +
                    " FROM WorkItems " +
                    " WHERE [System.TeamProject] = '" + userData.ProjectNameTFS + "'" +
                    " AND [Iteration Path] = '" + userData.IterationPathTFS + "'" +
                    " AND [Assigned To] = '" + userData.UserName + "'" +
                    " AND [Work Item Type] = 'Task'" +  //only Task -> Test Case doesn't have the same fields, causing query errors
                    " ORDER BY [System.Id], [System.WorkItemType]");

                return WIC;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public string CreateTaskOnTFS(string userName, string startDate, string title, string state, string description, string workItemLink, float chargeableHours = 7.5f, float nonchargeableHours = 0)
        {
            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            Uri tfsUri = new Uri(GetUrlTfs());

            NetworkCredential networkCredentials = new NetworkCredential(@"welfare\" + GetUserLogged(), GetUserLoggedPass());
            Microsoft.VisualStudio.Services.Common.WindowsCredential windowsCredentials = new Microsoft.VisualStudio.Services.Common.WindowsCredential(networkCredentials);
            VssCredentials basicCredentials = new VssCredentials(windowsCredentials);
            TfsTeamProjectCollection tfsColl = new TfsTeamProjectCollection(tfsUri, basicCredentials);
            WorkItemStore WIS = (WorkItemStore)tfsColl.GetService(typeof(WorkItemStore));
            tfsColl.Authenticate(); // make sure it is authenticate

            Project teamProject = WIS.Projects.GetById(3192); //3192 = BOM_MOD24 [35]
            WorkItemType workItemType = teamProject.WorkItemTypes["Task"];

            WorkItem newWI = new WorkItem(workItemType);
            newWI.Title = title;
            newWI.Fields["System.CreatedBy"].Value = GetUserLogged();
            newWI.Fields["System.AssignedTo"].Value = userName;
            newWI.Fields["System.TeamProject"].Value = projectName;
            newWI.Fields["Iteration Path"].Value = _iterationPath;
            newWI.Fields["Completed Work"].Value = chargeableHours ;
            newWI.Fields["Remaining Work"].Value = nonchargeableHours;
            newWI.Fields["Description"].Value = description;
            newWI.Fields["Start Date"].Value = Convert.ToDateTime(startDate);
            newWI.State = state;

            LinkWorkItem(ref newWI, workItemLink);

            var _valid = newWI.Validate();

            if (_valid.Count > 0)
            {
                throw new Exception("Errors when validating object [CreateTaskOnTFS]");
            }

            newWI.Save();

            return newWI.Fields["ID"].Value.ToString();
        }

        public int EditTaskOnTFS(int workItemNumber, string startDate, string title, float chargeableHours, float nonchargeableHours, string state, string description, string workItemLink)
        {
            Uri tfsUri = new Uri(GetUrlTfs());
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            WorkItemCollection WIC = WIS.Query(
                " SELECT [System.Id], " +
                " [System.WorkItemType], " +
                " [System.State], " +
                " [System.AssignedTo], " +
                " [System.Title], " +
                " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                " [Microsoft.VSTS.Scheduling.StartDate] " +
                " FROM WorkItems " +
                " WHERE [System.ID] = '" + workItemNumber + "'");

            WorkItem _wi = WIC.OfType<WorkItem>().FirstOrDefault();
            _wi.Open();
            _wi.Title = title;
            _wi.Fields["Completed Work"].Value = chargeableHours;
            _wi.Fields["Remaining Work"].Value = nonchargeableHours;
            _wi.Fields["Start Date"].Value = Convert.ToDateTime(startDate);
            _wi.Fields["Description"].Value = description;

            if (_wi.State.Equals("New") && state.Equals("Closed"))
            {
                _wi.State = "Active";

                var _validInternal = _wi.Validate();

                if (_validInternal.Count > 0)
                {
                    throw new Exception("Errors when validating object [EditTaskOnTFS]");
                }

                _wi.Save();
                return EditTaskOnTFS(workItemNumber, startDate, title, chargeableHours, nonchargeableHours, state, description, workItemLink);
            }

            _wi.State = state;

            LinkWorkItem(ref _wi, workItemLink);

            var _valid = _wi.Validate();

            if (_valid.Count > 0)
            {
                throw new Exception("Errors when validating object [EditTaskOnTFS]");
            }

            _wi.Save();

            return (int)_wi.Fields["ID"].Value;
        }

        private void LinkWorkItem(ref WorkItem _wi, string workItemLink)
        {
            IList<string> _workItemLink = workItemLink.Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
            var _workItemConverted = 0;
            var _flagCheckExistsWorItem = false;
            IList<RelatedLink> listToRemoveFromWILinks = new List<RelatedLink>();
            IList<string> listToRemoveFromNewWILinks = new List<string>();

            //Apply deletion for work items linked (when user remove work item link)
            foreach (Microsoft.TeamFoundation.WorkItemTracking.Client.Link linkItem in _wi.Links)
            {
                if (linkItem.BaseType == BaseLinkType.RelatedLink)            // WorkItem Link
                {
                    _flagCheckExistsWorItem = false;
                    foreach (var item in _workItemLink)
                    {
                        var trimItem = item.Trim();

                        if (((RelatedLink)linkItem).RelatedWorkItemId.ToString().Equals(trimItem))
                        {
                            //WORK ITEM ALREADY LINKED
                            _flagCheckExistsWorItem = true;
                            listToRemoveFromNewWILinks.Add(_workItemLink.Where(w => w == trimItem).FirstOrDefault());
                            break;
                        }
                    }

                    if (!_flagCheckExistsWorItem)
                    {
                        listToRemoveFromWILinks.Add((RelatedLink)linkItem);
                    }
                }
            }

            foreach (var item in listToRemoveFromWILinks)
            {
                //REMOVE WORK ITEMS NO LONGER IN THE LIST TO BE SAVED
                _wi.Links.Remove((RelatedLink)item);
            }

            //REMOVE WORK ITEMS ALREADY SAVED
            _workItemLink = _workItemLink.Except(listToRemoveFromNewWILinks).ToList();

            //ADD NEW WORK ITEMS LINKED
            foreach (var item in _workItemLink)
            {
                var trimItem = item.Trim();

                if (int.TryParse(trimItem, out _workItemConverted))
                {
                    try
                    {
                        _wi.Links.Add(new RelatedLink(_workItemConverted));
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw new Exception("Error while trying to add work item link: " + trimItem);
                }
            }
        }

        [HttpGet]
        public JsonResult GetWorkItemById(int workItemId)
        {
            var _urlTFS = GetUrlTfs();
            Uri tfsUri = new Uri(_urlTFS);
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            WorkItem workItem;

            try
            {
                workItem = WIS.GetWorkItem(workItemId);
            }
            catch (Exception ex)
            {
                throw new Exception("The work item does not exist, or you do not have permission to access it. (" + workItemId + ")");
            }

            var _workItemsLinked = "";
            for (int i = 0; i < workItem.WorkItemLinks.Count; i++)
            {
                _workItemsLinked += "#" + workItem.WorkItemLinks[i].TargetId + " ";
            }

            WorkItemSerialized _workItemSerialized = new WorkItemSerialized()
            {
                Id = workItem["Id"].ToString(),
                Title = workItem["Title"].ToString(),
                StartDate = workItem["Start Date"] != null ? (DateTime)workItem["Start Date"] : (DateTime?)null,
                Description = WebUtility.HtmlDecode(workItem["Description"].ToString()),
                CompletedHours = workItem["Completed Work"] != null ? (double)workItem["Completed Work"] : (double?)null,
                WorkItemsLinked = _workItemsLinked,
                State = workItem.State,
                CreationDate = workItem.CreatedDate,
                LinkUrl = _urlTFS + projectName + "/_queries?id=" + workItem["Id"].ToString()
            };
            return Json(_workItemSerialized, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetIdAndTitleWorkItemById(int workItemId)
        {
            var _urlTFS = GetUrlTfs();
            Uri tfsUri = new Uri(_urlTFS);
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            WorkItem workItem;

            try
            {
                workItem = WIS.GetWorkItem(workItemId);
            }
            catch (Exception ex)
            {
                throw new Exception("The work item does not exist, or you do not have permission to access it. (" + workItemId + ")");
            }

            var _workItemsLinked = "";
            for (int i = 0; i < workItem.WorkItemLinks.Count; i++)
            {
                _workItemsLinked += "#" + workItem.WorkItemLinks[i].TargetId + " ";
            }

            WorkItemSerialized _workItemSerialized = new WorkItemSerialized()
            {
                Id = workItem["Id"].ToString(),
                Title = workItem["Title"].ToString(),
                LinkUrl = _urlTFS + projectName + "/_queries?id=" + workItem["Id"].ToString()
            };
            return Json(_workItemSerialized, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetWorkItemByDay(string userName, DateTime day)
        {
            var _urlTFS = GetUrlTfs();
            Uri tfsUri = new Uri(_urlTFS);
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            userName = userName.Replace("'", "''");

            WorkItemCollection WIC = WIS.Query(
                " SELECT [System.Id], " +
                " [System.WorkItemType], " +
                " [System.State], " +
                " [System.AssignedTo], " +
                " [System.Title], " +
                " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                " [Microsoft.VSTS.Scheduling.StartDate] " +
                " FROM WorkItems " +
                " WHERE [System.TeamProject] = '" + projectName + "'" +
                " AND [Iteration Path] = '" + _iterationPath + "'" +
                " AND [Assigned To] = '" + userName + "'" +
                " AND [Start Date] = '" + day.ToShortDateString() + "'" +
                " ORDER BY [System.Id], [System.WorkItemType]");

            WorkItem workItem = WIC.OfType<WorkItem>().FirstOrDefault();

            var _workItemsLinked = "";
            WorkItemSerialized _workItemSerialized = new WorkItemSerialized();

            if (workItem != null)
            {
                for (int i = 0; i < workItem.WorkItemLinks.Count; i++)
                {
                    _workItemsLinked += "#" + workItem.WorkItemLinks[i].TargetId + " ";
                }

                _workItemSerialized.Id = workItem["Id"].ToString();
                _workItemSerialized.Title = workItem["Title"].ToString();
                _workItemSerialized.StartDate = workItem["Start Date"] != null ? (DateTime)workItem["Start Date"] : (DateTime?)null;
                _workItemSerialized.Description = WebUtility.HtmlDecode(workItem["Description"].ToString());
                _workItemSerialized.CompletedHours = workItem["Completed Work"] != null ? (double)workItem["Completed Work"] : (double?)null;
                _workItemSerialized.WorkItemsLinked = _workItemsLinked;
                _workItemSerialized.State = workItem.State;
                _workItemSerialized.CreationDate = workItem.CreatedDate;
                _workItemSerialized.LinkUrl = _urlTFS + projectName + "/_queries?id=" + workItem["Id"].ToString();
                return Json(_workItemSerialized, JsonRequestBehavior.AllowGet);
            }
            return Json(new EmptyResult(), JsonRequestBehavior.AllowGet);
        }

        [HttpPut]
        public bool CloseTasksMonth(string userName, int _month, int _year)
        {
            var _urlTFS = GetUrlTfs();
            Uri tfsUri = new Uri(_urlTFS);
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            userName = userName.Replace("'", "''");

            WorkItemCollection WIC = WIS.Query(
                " SELECT [System.Id], " +
                " [System.WorkItemType], " +
                " [System.State], " +
                " [System.AssignedTo], " +
                " [System.Title], " +
                " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                " [Microsoft.VSTS.Scheduling.StartDate] " +
                " FROM WorkItems " +
                " WHERE [System.TeamProject] = '" + projectName + "'" +
                " AND [Iteration Path] = '" + _iterationPath + "'" +
                " AND [Assigned To] = '" + userName + "'" +
                " ORDER BY [System.Id], [System.WorkItemType]");

            foreach (WorkItem _wi in WIC)
            {
                if (_wi["Start Date"] != null)
                {
                    DateTime _startDate = (DateTime)_wi["Start Date"];
                    if (_startDate.Month == (_month == 0 ? DateTime.Now.Month : _month)
                        && _startDate.Year == (_year == 0 ? DateTime.Now.Year : _year))
                    {

                        if (_wi.State.Equals("Closed"))
                        {
                            continue;
                        }

                        _wi.Open();

                        if (!_wi.State.Equals("Active"))
                        {
                            _wi.State = "Active";

                            var _validInternal = _wi.Validate();
                            if (_validInternal.Count > 0)
                            {
                                throw new Exception("Errors when validating object [CloseTasksMonth]");
                            }

                            _wi.Save();
                            _wi.State = "Closed";
                            _wi.Save();
                        }
                        else
                        {
                            var _validInternal = _wi.Validate();
                            if (_validInternal.Count > 0)
                            {
                                throw new Exception("Errors when validating object [CloseTasksMonth]");
                            }
                            _wi.State = "Closed";
                            _wi.Save();
                        }
                    }
                }
            }

            return true;
        }

        [HttpPost]
        public int ApplyCreationDateToStartDate(string userName)
        {
            Uri tfsUri = new Uri(GetUrlTfs());
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();
            userName = userName.Replace("'", "''");

            WorkItemCollection WIC = WIS.Query(
                " SELECT [System.Id], " +
                " [System.WorkItemType], " +
                " [System.State], " +
                " [System.AssignedTo], " +
                " [System.Title], " +
                " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                " [Microsoft.VSTS.Scheduling.StartDate] " +
                " FROM WorkItems " +
                " WHERE [System.TeamProject] = '" + projectName + "'" +
                " AND [Iteration Path] = '" + _iterationPath + "'" +
                " AND [Assigned To] = '" + userName + "'" +
                " AND [Work Item Type] = 'Task'" +  //only Task -> Test Case doesn't have the same fields, causing query errors
                " ORDER BY [System.Id], [System.WorkItemType]");

            var _count = 0;

            foreach (WorkItem wi in WIC)
            {
                if (wi["Start Date"] == null)
                {
                    wi.Open();
                    wi.Fields["Start Date"].Value = wi.CreatedDate;

                    var _valid = wi.Validate();

                    if (_valid.Count > 0)
                    {
                        throw new Exception("Errors when validating object [ApplyCreationDateToStartDate]");
                    }

                    wi.Save();

                    _count++;
                }
            }

            return _count;
        }

        [HttpGet]
        public bool WorkItemExists(int workItemId)
        {
            var _urlTFS = GetUrlTfs();
            Uri tfsUri = new Uri(_urlTFS);
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            WorkItem workItem;

            try
            {
                workItem = WIS.GetWorkItem(workItemId);
                if (workItem != null)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion TFS

        #region EXCEL

        #region UTIL

        public string NameSplittedByUnderscore(string name, string separator)
        {
            return name.Replace(" ", separator);
        }

        public string TimesheetFileName(string userName, char separator, int _month, int _year)
        {
            var _monthFormatted = new DateTime(_year, _month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            return "Timesheet" + separator + NameSplittedByUnderscore(userName, separator.ToString()) + separator + _monthFormatted + separator + _year;
        }

        [HttpGet]
        public string TimesheetSaveLocation()
        {
            var _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";
            var _path = ConfigurationManager.AppSettings["pathTimesheet"];
            return _path == null ? _desktop : (Directory.Exists(_path.ToString()) ? _path.ToString() : _desktop);
        }

        [HttpGet]
        public string TimesheetSaveLocationAndFileName(string userName, int _month, int _year)
        {
            return TimesheetSaveLocation() + TimesheetFileName(userName.Replace("'", ""), '_', _month, _year);
        }

        public void SetCellValue(Worksheet worksheet, CellObject cellObj)
        {
            worksheet.get_Range(cellObj.CellPosition).Value = cellObj.CellValue;
        }

        public void FormatCell(Worksheet worksheet, CellObject cellObj)
        {
            worksheet.get_Range(cellObj.CellPosition).Font.Size = cellObj.FormatParams.fontSize;
            worksheet.get_Range(cellObj.CellPosition).Font.Bold = cellObj.FormatParams.fontBold;
            worksheet.get_Range(cellObj.CellPosition).Font.Italic = cellObj.FormatParams.fontItalic;
            worksheet.get_Range(cellObj.CellPosition).Font.Color = cellObj.FormatParams.fontColor;
            worksheet.get_Range(cellObj.CellPosition).HorizontalAlignment = cellObj.FormatParams.aligment;
            worksheet.get_Range(cellObj.CellPosition).Locked = cellObj.FormatParams.lockCell;
            //worksheet.get_Range(cellObj.CellPosition).Interior.Color = cellObj.FormatParams.cellBackgroundColor;
        }

        public string GetUserLogged()
        {
            return Session["userLogged"] as string;
        }

        public string GetUserLoggedName()
        {
            return Session["userLoggedName"] as string;
        }

        public string GetUserLoggedPass()
        {
            //criptograph before saving
            return Session["userLoggedPass"] as string;
        }

        public void FormatTasksForEmailConfirmation(UserDataSearchTFS userData)
        {

            var timesheetTasks = TimesheetRecords(userData);

            StringBuilder strMain = new StringBuilder();
            StringBuilder strOutOfOffice = new StringBuilder();
            StringBuilder strUnderWork = new StringBuilder();
            StringBuilder strOverWork = new StringBuilder();
            StringBuilder strNormalWork = new StringBuilder();

            strMain.AppendLine("--------------------------------");
            strMain.AppendLine("Member Name: " + userData.UserName);
            strMain.AppendLine("--------------------------------");

            foreach (var item in timesheetTasks)
            {

                if (item.IsWeekend)
                {
                    strMain.AppendLine();
                    strMain.AppendLine(item.Date.ToString("dddd dd/MM/yyyy"));
                    continue;
                }

                if (item.Description.Equals("Out of the office"))
                {
                    strMain.AppendLine();
                    strMain.AppendLine(item.Description + " - " + item.Date.ToString("dddd dd/MM/yyyy"));
                    continue;
                }

                strMain.AppendLine();
                strMain.AppendLine("Title: " + item.Description);
                strMain.AppendLine("Start Date: " + item.Date.ToShortDateString());
                strMain.AppendLine("Completed Hours: " + item.ChargeableHours);

                if (item.NonChargeableHours > 0)
                    strMain.AppendLine("Non-Chargeable Hours: " + item.NonChargeableHours);
            }

            var str = strMain.ToString();
        }

        #endregion UTIL

        #region Const Cell Range and Labels

        public string UserName { get; set; }
        public string MonthTimesheet { get; set; }
        public string YearTimesheet { get; set; }

        // --------------------------------------- INIT HEADER TIMESHEET -----------------------------------------

        //PROJECT NAME

        #region CellNames
        public CellObject CellProjectNameLabel { get; set; }
        public CellObject CellProjectNameInput { get; set; }

        //Period
        public CellObject CellPeriodLabel { get; set; }
        public CellObject CellPeriodInput { get; set; }

        //NAME
        public CellObject CellNameLabel { get; set; }
        public CellObject CellNameInput { get; set; }

        //Total working days in month
        public CellObject CellTotalWorkingDaysInMonthLabel { get; set; }
        public CellObject CellTotalWorkingDaysInMonthInput { get; set; }

        //Total hours
        public CellObject CellTotalHoursLabel { get; set; }
        public CellObject CellTotalHoursInput { get; set; }

        //Total Chargeable hours
        public CellObject CellTotalChargeableHoursLabel { get; set; }
        public CellObject CellTotalChargeableHoursInput { get; set; }

        //Total Non-Chargeable hours
        public CellObject CellTotalNonChargeableHoursLabel { get; set; }
        public CellObject CellTotalNonChargeableHoursInput { get; set; }

        // --------------------------------------- END HEADER TIMESHEET -----------------------------------------

        //---------------------------------------- INIT HEADER TABLE---------------------------------------------

        //Header Table Id
        public CellObject CellHeaderTableId { get; set; }

        //Header Table Date
        public CellObject CellHeaderTableDate { get; set; }

        //Header Table WorkItem
        public CellObject CellHeaderTableWorkItem { get; set; }

        //Header Table Description
        public CellObject CellHeaderTableDescription { get; set; }

        //Header Table Chargeable Hours
        public CellObject CellHeaderTableChargeableHours { get; set; }

        //Header Table Non-Chargeable Hours
        public CellObject CellHeaderTableNonChargeableHours { get; set; }

        //Header Table Comments
        public CellObject CellHeaderTableComments { get; set; }
        #endregion CellNames

        //---------------------------------------- END HEADER TABLE---------------------------------------------

        #endregion Const Cell Range and Labels

        public void InitExcelVariables(string userName, int _month, int _year)
        {
            UserName = userName;
            MonthTimesheet = new DateTime(_year, _month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            YearTimesheet = _year.ToString();

            CellProjectNameLabel = new CellObject
            {
                CellPosition = "B1",
                CellValue = "Project Name",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellProjectNameInput = new CellObject
            {
                CellPosition = "C1",
                CellValue = "BOMi Modelling Team", //TODO DYNAMIC
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = false,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellPeriodLabel = new CellObject
            {
                CellPosition = "B2",
                CellValue = "Period",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellPeriodInput = new CellObject
            {
                CellPosition = "C2",
                CellValue = "'" + new DateTime(_year, _month, 1).ToString("MMM/yyyy", CultureInfo.CreateSpecificCulture("en")), //TODO DYNAMIC
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = false,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellNameLabel = new CellObject
            {
                CellPosition = "B3",
                CellValue = "Name",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellNameInput = new CellObject
            {
                CellPosition = "C3",
                CellValue = UserName, //TODO DYNAMIC
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = false,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignLeft,
                    lockCell = true
                }
            };

            CellTotalWorkingDaysInMonthLabel = new CellObject
            {
                CellPosition = "D1",
                CellValue = "Total Working Days In Month",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalWorkingDaysInMonthInput = new CellObject
            {
                CellPosition = "E1",
                CellValue = "=E3/7.5", //TODO: apply formula
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalHoursLabel = new CellObject
            {
                CellPosition = "D2",
                CellValue = "Total Hours",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalHoursInput = new CellObject
            {
                CellPosition = "E2",
                CellValue = "=SUM(E3:E4)", //TODO: apply formula
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalChargeableHoursLabel = new CellObject
            {
                CellPosition = "D3",
                CellValue = "Total Chargeable Hours",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalChargeableHoursInput = new CellObject
            {
                CellPosition = "E3",
                //CellValue = "=SUM(E6:E50)", //TODO: apply formula // it gets set in CreateTable method (868)
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalNonChargeableHoursLabel = new CellObject
            {
                CellPosition = "D4",
                CellValue = "Total Non-Chargeable Hours",
                //(Hours worked in excess of agreed daily working hours or non chargeable days as agreed)
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellTotalNonChargeableHoursInput = new CellObject
            {
                CellPosition = "E4",
                //CellValue = "=SUM(F6:F50)", //TODO: apply formula // it gets set in CreateTable method (868)
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 11,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignRight,
                    lockCell = true
                }
            };

            CellHeaderTableId = new CellObject
            {
                CellPosition = "A5",
                CellValue = "Id",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableDate = new CellObject
            {
                CellPosition = "B5",
                CellValue = "Date",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableWorkItem = new CellObject
            {
                CellPosition = "C5",
                CellValue = "Work Item",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableDescription = new CellObject
            {
                CellPosition = "D5",
                CellValue = "Title",//"Description",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableChargeableHours = new CellObject
            {
                CellPosition = "E5",
                CellValue = "Chargeable Hours",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableNonChargeableHours = new CellObject
            {
                CellPosition = "F5",
                CellValue = "Non-Charg. Hours", //"Non-Chargeable Hours",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };

            CellHeaderTableComments = new CellObject
            {
                CellPosition = "G5",
                CellValue = "Work Items Linked", //"Comments",
                FormatParams = new ParamsFormatCell()
                {
                    fontSize = 12,
                    fontBold = true,
                    fontItalic = false,
                    fontColor = Color.Black,
                    cellBackgroundColor = Color.Transparent,
                    aligment = XlHAlign.xlHAlignCenter,
                    lockCell = true
                }
            };
        }

        private void CreateHeader(Worksheet worksheet)
        {
            //-------------------------------- INIT TIMESHEET HEADER -----------------------------------------------
            SetValAndFormatCell(worksheet, CellProjectNameLabel);//B1 - Project Name
            SetValAndFormatCell(worksheet, CellProjectNameInput);//C1 - Project Name
            SetValAndFormatCell(worksheet, CellPeriodLabel);//B2 - Month
            SetValAndFormatCell(worksheet, CellPeriodInput);//C2 - Month
            SetValAndFormatCell(worksheet, CellNameLabel);//B3 - Name
            SetValAndFormatCell(worksheet, CellNameInput);//C3 - Name
            //----------------------------------------------------------------------------------------------
            SetValAndFormatCell(worksheet, CellTotalWorkingDaysInMonthLabel);//D1 - Total working days in month
            SetValAndFormatCell(worksheet, CellTotalWorkingDaysInMonthInput);//E1 - Total working days in month
            SetValAndFormatCell(worksheet, CellTotalHoursLabel);//D2 - Total Hours
            SetValAndFormatCell(worksheet, CellTotalHoursInput);//E2 - Total Hours
            SetValAndFormatCell(worksheet, CellTotalChargeableHoursLabel);//D3 - Total Chargeable Hours
            SetValAndFormatCell(worksheet, CellTotalChargeableHoursInput);//E3 - Total Chargeable Hours
            SetValAndFormatCell(worksheet, CellTotalNonChargeableHoursLabel);//D4 - Total Non-Chargeable Hours
            SetValAndFormatCell(worksheet, CellTotalNonChargeableHoursInput);//E4 - Total Non-Chargeable Hours
            //-------------------------------- END TIMESHEET HEADER -----------------------------------------------

            //---------------------------------------- INIT HEADER TABLE---------------------------------------------
            SetValAndFormatCell(worksheet, CellHeaderTableId);
            SetValAndFormatCell(worksheet, CellHeaderTableDate);
            SetValAndFormatCell(worksheet, CellHeaderTableWorkItem);
            SetValAndFormatCell(worksheet, CellHeaderTableDescription);
            SetValAndFormatCell(worksheet, CellHeaderTableChargeableHours);
            SetValAndFormatCell(worksheet, CellHeaderTableNonChargeableHours);
            SetValAndFormatCell(worksheet, CellHeaderTableComments);
            //---------------------------------------- END HEADER TABLE---------------------------------------------
        }

        private int CreateTable_DELETE(Worksheet worksheet, UserDataSearchTFS userData)
        {
            var _table = TimesheetRecords(userData);
            int firstRow = 6;
            int lastRow = _table.Count + firstRow;
            int j;

            worksheet.get_Range(CellTotalChargeableHoursInput.CellPosition).Value = "=SUM(E" + firstRow + ":E" + lastRow + ")";
            worksheet.get_Range(CellTotalNonChargeableHoursInput.CellPosition).Value = "=SUM(F" + firstRow + ":F" + lastRow + ")";

            for (int i = firstRow; i < lastRow; i++) //ROW
            {
                j = i - firstRow;
                worksheet.Cells[i, 1] = _table[j].Id;
                worksheet.Cells[i, 2] = _table[j].Date.ToShortDateString();
                worksheet.Cells[i, 3] = _table[j].WorkItemNumber;
                worksheet.Cells[i, 4] = _table[j].Description;
                worksheet.Cells[i, 5] = _table[j].ChargeableHours;
                worksheet.Cells[i, 6] = _table[j].NonChargeableHours;
                worksheet.Cells[i, 7] = _table[j].Comments;

                worksheet.Cells[i, 2].NumberFormat = "MM/DD/YYYY"; //date in american format
                worksheet.Cells[i, 3].NumberFormat = "0"; //workitem formatted as number

                if (_table[j].IsWeekend)
                {
                    //worksheet.Cells[i, 3] = ""; //WorkItemNumber
                    //worksheet.Cells[i, 5] = ""; //ChargeableHours
                    //worksheet.Cells[i, 6] = ""; //NonChargeableHours
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.DarkGray; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = true;
                }
                else
                {
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.White; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = false;
                }
            }

            worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].HorizontalAlignment = XlHAlign.xlHAlignCenter;
            worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].VerticalAlignment = XlHAlign.xlHAlignCenter;

            worksheet.Cells[1, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Working Days In Month)
            worksheet.Cells[2, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Hours)
            worksheet.Cells[3, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Chargeable Hours)
            worksheet.Cells[4, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Non-Chargeable Hours)

            return _table.Count;
        }

        private int CreateTable(Worksheet worksheet, UserDataSearchTFS userData)
        {
            var _table = TimesheetRecords(userData);
            int firstRow = 6;
            int lastRow = _table.Count + firstRow;
            int j;

            worksheet.get_Range(CellTotalChargeableHoursInput.CellPosition).Value = "=SUM(E" + firstRow + ":E" + lastRow + ")";
            worksheet.get_Range(CellTotalNonChargeableHoursInput.CellPosition).Value = "=SUM(F" + firstRow + ":F" + lastRow + ")";

            for (int i = firstRow; i < lastRow; i++) //ROW
            {
                j = i - firstRow;
                worksheet.Cells[i, 1] = _table[j].Id;
                worksheet.Cells[i, 2] = _table[j].Date.ToShortDateString();
                worksheet.Cells[i, 3] = _table[j].WorkItemNumber;
                worksheet.Cells[i, 4] = _table[j].Description;
                worksheet.Cells[i, 5] = _table[j].ChargeableHours;
                worksheet.Cells[i, 6] = _table[j].NonChargeableHours;
                worksheet.Cells[i, 7] = _table[j].Comments;

                worksheet.Cells[i, 2].NumberFormat = "MM/DD/YYYY"; //date in american format
                worksheet.Cells[i, 3].NumberFormat = "0"; //workitem formatted as number

                if (_table[j].IsWeekend)
                {
                    //worksheet.Cells[i, 3] = ""; //WorkItemNumber
                    //worksheet.Cells[i, 5] = ""; //ChargeableHours
                    //worksheet.Cells[i, 6] = ""; //NonChargeableHours
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.DarkGray; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = true;
                }
                else
                {
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.White; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = false;
                }
            }

            worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].HorizontalAlignment = XlHAlign.xlHAlignCenter;
            worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].VerticalAlignment = XlHAlign.xlHAlignCenter;

            worksheet.Cells[1, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Working Days In Month)
            worksheet.Cells[2, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Hours)
            worksheet.Cells[3, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Chargeable Hours)
            worksheet.Cells[4, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Non-Chargeable Hours)

            return _table.Count;
        }

        public IList<WorkItemRecord> Convert_TFS_Events_To_Excel_Format(ConsolidatedMonthUserData consolidatedMonthUserData, int _month, int _year)
        {
            IList<WorkItemRecord> timesheetRecords = new List<WorkItemRecord>();
            var _days = DateTime.DaysInMonth(_year, _month);
            DateTime day;
            var countTasks = 0;
            //loop for all days in the month but doesn't include weekends
            //allow more then 1 task per day
            for (int i = 0; i < _days; i++) //_events.Count; i++) //
            {

                day = new DateTime(_year, _month, i + 1);

                var _itemEvent = consolidatedMonthUserData.ListWorkItem.Where(x => x.StartDate.Value.ToShortDateString() == day.ToShortDateString());
                if (_itemEvent.Count() > 0)
                {
                    var _currentChargeableHours = 0.0;
                    var _currentNonChargeableHours = 0.0;
                    //var totalChargeableHoursRemaining = 7.5;

                    foreach (var item in _itemEvent)
                    {

                        if (IsWeekend(day))
                        {
                            _currentChargeableHours = 0;
                            _currentNonChargeableHours = item.CompletedHours.Value + item.RemainingWork.Value;
                        }
                        else
                        {
                            _currentChargeableHours = item.CompletedHours.HasValue ? item.CompletedHours.Value : 0;
                            _currentNonChargeableHours = item.RemainingWork.HasValue ?  item.RemainingWork.Value : 0;
                        }

                        //else
                        //{

                        #region Calculate Chargeable and Non-Chargeable hours
                        //if (item.CompletedHours.HasValue)
                        //{
                        //    if (totalChargeableHoursRemaining == 0)
                        //    {
                        //        _currentChargeableHours = 0;
                        //        _currentNonChargeableHours = item.CompletedHours.Value;
                        //    }
                        //    else
                        //    {
                        //        if (item.CompletedHours.Value > 7.5)
                        //        {

                        //            _currentChargeableHours = 7.5;
                        //            _currentNonChargeableHours = item.CompletedHours.Value - totalChargeableHoursRemaining;// 7.5;
                        //        }
                        //        else
                        //        {
                        //            _currentChargeableHours = item.CompletedHours.Value;
                        //        }

                        //        if (totalChargeableHoursRemaining > 0)
                        //        {
                        //            if (totalChargeableHoursRemaining >= _currentChargeableHours)
                        //            {
                        //                totalChargeableHoursRemaining = totalChargeableHoursRemaining - _currentChargeableHours;
                        //            }
                        //            else
                        //            {
                        //                _currentNonChargeableHours = item.CompletedHours.Value - totalChargeableHoursRemaining;
                        //                _currentChargeableHours = totalChargeableHoursRemaining;
                        //                totalChargeableHoursRemaining = 0;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            _currentNonChargeableHours = _currentChargeableHours;
                        //            _currentChargeableHours = 0;
                        //        }
                        //    }
                        //}
                        #endregion Calculate Chargeable and Non-Chargeable hours
                        //}

                        timesheetRecords.Add(new WorkItemRecord
                        {
                            Id = ++countTasks,
                            Date = item.StartDate.Value,
                            WorkItemNumber = int.Parse(item.Id),
                            Description = item.Title, //item.Description,
                            ChargeableHours = (float)item.CompletedHours,
                            NonChargeableHours = (float)item.RemainingWork,
                            Comments = item.WorkItemsLinked,
                            IsWeekend = IsWeekend(item.StartDate.Value)
                        });

                    }
                }
                else //does not exist this date in the events
                {
                    var _description = "Out of the office";
                    if (IsWeekend(day))
                    {
                        _description = "Weekend";
                    }
                    timesheetRecords.Add(new WorkItemRecord
                    {
                        Id = ++countTasks,
                        Date = day,
                        WorkItemNumber = 0,
                        Description = _description,
                        ChargeableHours = 0,
                        NonChargeableHours = 0,
                        Comments = "------",
                        IsWeekend = IsWeekend(day)
                    });
                }
            }
            return timesheetRecords;
        }
        public IList<WorkItemRecord> Convert_TFS_Events_To_Excel_Format_DELETE(IList<WorkItemSerialized> _events, int _month, int _year)
        {
            IList<WorkItemRecord> timesheetRecords = new List<WorkItemRecord>();
            var _days = DateTime.DaysInMonth(_year, _month);
            DateTime day;
            var countTasks = 0;
            //loop for all days in the month but doesn't include weekends
            //allow more then 1 task per day
            for (int i = 0; i < _days; i++) //_events.Count; i++) //
            {

                day = new DateTime(_year, _month, i + 1);

                var _itemEvent = _events.Where(x => x.StartDate.Value.ToShortDateString() == day.ToShortDateString());
                if (_itemEvent.Count() > 0)
                {
                    var _currentChargeableHours = 0.0;
                    var _currentNonChargeableHours = 0.0;
                    var totalChargeableHoursRemaining = 7.5;

                    foreach (var item in _itemEvent)
                    {

                        if (IsWeekend(day))
                        {
                            _currentChargeableHours = 0;
                            _currentNonChargeableHours = item.CompletedHours.Value;
                        }
                        else
                        {

                            #region Calculate Chargeable and Non-Chargeable hours
                            if (item.CompletedHours.HasValue)
                            {
                                if (totalChargeableHoursRemaining == 0)
                                {
                                    _currentChargeableHours = 0;
                                    _currentNonChargeableHours = item.CompletedHours.Value;
                                }
                                else
                                {
                                    if (item.CompletedHours.Value > 7.5)
                                    {

                                        _currentChargeableHours = 7.5;
                                        _currentNonChargeableHours = item.CompletedHours.Value - totalChargeableHoursRemaining;// 7.5;
                                    }
                                    else
                                    {
                                        _currentChargeableHours = item.CompletedHours.Value;
                                    }

                                    if (totalChargeableHoursRemaining > 0)
                                    {
                                        if (totalChargeableHoursRemaining >= _currentChargeableHours)
                                        {
                                            totalChargeableHoursRemaining = totalChargeableHoursRemaining - _currentChargeableHours;
                                        }
                                        else
                                        {
                                            _currentNonChargeableHours = item.CompletedHours.Value - totalChargeableHoursRemaining;
                                            _currentChargeableHours = totalChargeableHoursRemaining;
                                            totalChargeableHoursRemaining = 0;
                                        }
                                    }
                                    else
                                    {
                                        _currentNonChargeableHours = _currentChargeableHours;
                                        _currentChargeableHours = 0;
                                    }
                                }
                            }
                            #endregion Calculate Chargeable and Non-Chargeable hours
                        }

                        timesheetRecords.Add(new WorkItemRecord
                        {
                            Id = ++countTasks,
                            Date = item.StartDate.Value,
                            WorkItemNumber = int.Parse(item.Id),
                            Description = item.Title, //item.Description,
                            ChargeableHours = (float)_currentChargeableHours,
                            NonChargeableHours = (float)_currentNonChargeableHours + (item.RemainingWork != null ? (float)item.RemainingWork : 0),
                            Comments = item.WorkItemsLinked,
                            IsWeekend = IsWeekend(item.StartDate.Value)
                        });

                    }
                }
                else //does not exist this date in the events
                {
                    var _description = "Out of the office";
                    if (IsWeekend(day))
                    {
                        _description = "Weekend";
                    }
                    timesheetRecords.Add(new WorkItemRecord
                    {
                        Id = ++countTasks,
                        Date = day,
                        WorkItemNumber = 0,
                        Description = _description,
                        ChargeableHours = 0,
                        NonChargeableHours = 0,
                        Comments = "------",
                        IsWeekend = IsWeekend(day)
                    });
                }
            }
            return timesheetRecords;
        }

        public void SetValAndFormatCell(Worksheet worksheet, CellObject cellObj)
        {
            SetCellValue(worksheet, cellObj);
            FormatCell(worksheet, cellObj);
        }

        //[HttpPost]
        //public string SaveExcelFile(UserDataSearchTFS userData)
        //{
        //    Application excel;
        //    Workbook worKbooK;
        //    Worksheet worksheet;
        //    Range celLrangE;

        //    try
        //    {
        //        userData.UserName = userData.UserName.Replace("'", "''");

        //        excel = new Application();
        //        excel.Visible = false;
        //        excel.DisplayAlerts = false;
        //        worKbooK = excel.Workbooks.Add(Type.Missing);
        //        var tableEventCount = 0;

        //        InitExcelVariables(userData.UserName, userData.Month, userData.Year);

        //        worksheet = (Worksheet)worKbooK.ActiveSheet;
        //        worksheet.Name = "Timesheet_" + userData.UserName;//.Replace(" ", "_");

        //        CreateHeader(worksheet);
        //        tableEventCount = CreateTable(worksheet, userData);
        //        resizeColumns(worksheet);

        //        var borderStartsRow = 5;
        //        var borderEndsRow = borderStartsRow + tableEventCount;
        //        celLrangE = worksheet.Range[worksheet.Cells[borderStartsRow, 1], worksheet.Cells[borderEndsRow, 7]]; //TODO
        //        Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
        //        border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //        border.Weight = 2d;

        //        protectSheet(worksheet);
        //        var saveParams = TimesheetSaveLocationAndFileName(userData.UserName, userData.Month, userData.Year);
        //        worKbooK.SaveAs(saveParams);

        //        worKbooK.Close();
        //        excel.Workbooks.Close();
        //        excel.Quit();
        //        Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
        //        Marshal.ReleaseComObject(worKbooK);
        //        return "File saved sucessfully!"; // - Path: " + saveParams;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Write(ex.Message);
        //        return "Interal Code (1) - " + ex.Message;
        //    }
        //    finally
        //    {
        //        worksheet = null;
        //        celLrangE = null;
        //        worKbooK = null;

        //    }
        //}

        //[HttpPost]
        //public string BulkSaveExcelFile(IList<UserDataSearchTFS> userData)
        //{
        //    Application excel;
        //    Workbook worKbooK;
        //    Worksheet worksheet;
        //    Range celLrangE;
        //    StringBuilder result = new StringBuilder();
        //    foreach (var item in userData)
        //    {
        //        try
        //        {
        //            item.UserName = item.UserName.Replace("'", "''");

        //            excel = new Application();
        //            excel.Visible = false;
        //            excel.DisplayAlerts = false;
        //            worKbooK = excel.Workbooks.Add(Type.Missing);
        //            var tableEventCount = 0;

        //            InitExcelVariables(item.UserName, item.Month, item.Year);

        //            worksheet = (Worksheet)worKbooK.ActiveSheet;
        //            worksheet.Name = "Timesheet_" + item.UserName;//.Replace(" ", "_");

        //            CreateHeader(worksheet);
        //            tableEventCount = CreateTable(worksheet, item);
        //            resizeColumns(worksheet);

        //            var borderStartsRow = 5;
        //            var borderEndsRow = borderStartsRow + tableEventCount;
        //            celLrangE = worksheet.Range[worksheet.Cells[borderStartsRow, 1], worksheet.Cells[borderEndsRow, 7]]; //TODO
        //            Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
        //            border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //            border.Weight = 2d;

        //            protectSheet(worksheet);
        //            var saveParams = TimesheetSaveLocationAndFileName(item.UserName, item.Month, item.Year);
        //            worKbooK.SaveAs(saveParams);

        //            result.Append($"File saved sucessfully!({worksheet.Name})"); // - Path: " + saveParams;
        //            result.Append("<br />");
        //            worKbooK.Close();
        //            excel.Workbooks.Close();
        //            excel.Quit();
        //            Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
        //            Marshal.ReleaseComObject(worKbooK);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.Write(ex.Message);
        //            result.AppendLine($"Interal Code (1) - {ex.Message}");
        //            result.Append("<br />");
        //        }
        //        finally
        //        {
        //            worksheet = null;
        //            celLrangE = null;
        //            worKbooK = null;

        //        }
        //    }
        //    return result.ToString();
        //}

        [HttpPost]
        public string SaveExcelFile(UserDataSearchTFS userData)
        {
            Application excel;
            Workbook worKbooK;
            Worksheet worksheet;
            Range celLrangE;

            try
            {
                userData.UserName = userData.UserName.Replace("'", "''");

                excel = new Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);
                var tableEventCount = 0;

                InitExcelVariables(userData.UserName, userData.Month, userData.Year);

                worksheet = (Worksheet)worKbooK.ActiveSheet;
                worksheet.Name = "Timesheet_" + userData.UserName;//.Replace(" ", "_");

                CreateHeader(worksheet);
                tableEventCount = CreateTable(worksheet, userData);
                resizeColumns(worksheet);

                var borderStartsRow = 5;
                var borderEndsRow = borderStartsRow + tableEventCount;
                celLrangE = worksheet.Range[worksheet.Cells[borderStartsRow, 1], worksheet.Cells[borderEndsRow, 7]]; //TODO
                Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
                border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                border.Weight = 2d;

                protectSheet(worksheet);
                var saveParams = TimesheetSaveLocationAndFileName(userData.UserName, userData.Month, userData.Year);
                worKbooK.SaveAs(saveParams);

                worKbooK.Close();
                excel.Workbooks.Close();
                excel.Quit();
                Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
                Marshal.ReleaseComObject(worKbooK);
                return "File saved sucessfully!"; // - Path: " + saveParams;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return "Interal Code (1) - " + ex.Message;
            }
            finally
            {
                worksheet = null;
                celLrangE = null;
                worKbooK = null;

            }
        }

        [HttpPost]
        public string BulkSaveExcelFile(IList<UserDataSearchTFS> userData)
        {
            Application excel;
            Workbook worKbooK;
            Worksheet worksheet;
            Range celLrangE;
            StringBuilder result = new StringBuilder();
            foreach (var item in userData)
            {
                try
                {
                    item.UserName = item.UserName.Replace("'", "''");

                    excel = new Application();
                    excel.Visible = false;
                    excel.DisplayAlerts = false;
                    worKbooK = excel.Workbooks.Add(Type.Missing);
                    var tableEventCount = 0;

                    InitExcelVariables(item.UserName, item.Month, item.Year);

                    worksheet = (Worksheet)worKbooK.ActiveSheet;
                    worksheet.Name = "Timesheet_" + item.UserName;//.Replace(" ", "_");

                    CreateHeader(worksheet);
                    tableEventCount = CreateTable(worksheet, item);
                    resizeColumns(worksheet);

                    var borderStartsRow = 5;
                    var borderEndsRow = borderStartsRow + tableEventCount;
                    celLrangE = worksheet.Range[worksheet.Cells[borderStartsRow, 1], worksheet.Cells[borderEndsRow, 7]]; //TODO
                    Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
                    border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    border.Weight = 2d;

                    protectSheet(worksheet);
                    var saveParams = TimesheetSaveLocationAndFileName(item.UserName, item.Month, item.Year);
                    worKbooK.SaveAs(saveParams);

                    result.Append($"File saved sucessfully!({worksheet.Name})"); // - Path: " + saveParams;
                    result.Append("<br />");
                    worKbooK.Close();
                    excel.Workbooks.Close();
                    excel.Quit();
                    Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
                    Marshal.ReleaseComObject(worKbooK);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    result.AppendLine($"Interal Code (1) - {ex.Message}");
                    result.Append("<br />");
                }
                finally
                {
                    worksheet = null;
                    celLrangE = null;
                    worKbooK = null;

                }
            }
            return result.ToString();
        }

        //[HttpPost]
        //public FileResult SaveExcelFile(UserDataSearchTFS userData)
        //{
        //    Application excel;
        //    Workbook worKbooK;
        //    Worksheet worksheet;
        //    Range celLrangE;

        //    try
        //    {
        //        userData.UserName = userData.UserName.Replace("'", "''");

        //        excel = new Application();
        //        excel.Visible = false;
        //        excel.DisplayAlerts = false;
        //        worKbooK = excel.Workbooks.Add(Type.Missing);
        //        var tableEventCount = 0;

        //        InitExcelVariables(userData.UserName, userData.Month, userData.Year);

        //        worksheet = (Worksheet)worKbooK.ActiveSheet;
        //        worksheet.Name = "Timesheet_" + userData.UserName;//.Replace(" ", "_");

        //        CreateHeader(worksheet);
        //        tableEventCount = CreateTable(worksheet, userData);
        //        resizeColumns(worksheet);

        //        var borderStartsRow = 5;
        //        var borderEndsRow = borderStartsRow + tableEventCount;
        //        celLrangE = worksheet.Range[worksheet.Cells[borderStartsRow, 1], worksheet.Cells[borderEndsRow, 7]]; //TODO
        //        Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
        //        border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //        border.Weight = 2d;

        //        protectSheet(worksheet);
        //        var saveParams = TimesheetSaveLocationAndFileName(userData.UserName, userData.Month, userData.Year);
        //        //worKbooK.SaveAs(saveParams);

        //        MemoryStream ms = new MemoryStream();
        //        worKbooK.SaveAs(ms);//, saveParams);
        //        ms.Seek(0, SeekOrigin.Begin);

        //        byte[] buffer = new byte[(int)ms.Length];
        //        buffer = ms.ToArray();

        //        worKbooK.Close();
        //        excel.Workbooks.Close();
        //        excel.Quit();
        //        Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
        //        Marshal.ReleaseComObject(worKbooK);
        //        //return "File saved sucessfully!"; // - Path: " + saveParams;

        //        using (MemoryStream stream = new MemoryStream())
        //        {
        //            string fileName = TimesheetFileName(userData.UserName.Replace("'", ""), '_', userData.Month, userData.Year);// "myfile.ext";
        //            return File(buffer, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        //            //return File(stream, "application/vnd.ms-excel");
        //        }

        //        //MemoryStream m = new MemoryStream();
        //        ////worKbooK.SaveToStream(m);
        //        //using (MemoryStream stream = new MemoryStream())
        //        //{
        //        //    worKbooK.SaveAs(stream);
        //        //    return File(stream, "application/vnd.ms-excel");
        //        //}

        //        //return File(m, "application/vnd.ms-excel");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Write(ex.Message);
        //        return null;// "Interal Code (1) - " + ex.Message;
        //    }
        //    finally
        //    {
        //        worksheet = null;
        //        celLrangE = null;
        //        worKbooK = null;

        //    }
        //}

        //--------------------------------------------------------------------------------------------------------

        private void WorkbookSheetChange(Workbook workbook)
        {
            workbook.SheetChange += new
                WorkbookEvents_SheetChangeEventHandler(
                ThisWorkbook_SheetChange);
        }

        void ThisWorkbook_SheetChange(object Sh, Range Target)
        {
            Worksheet sheet = (Worksheet)Sh;

            string changedRange = Target.get_Address(
                XlReferenceStyle.xlA1);
        }

        //--------------------------------------------------------------------------------------------------------

        private void protectSheet(Worksheet worksheet)
        {
            var missing = Type.Missing;
            //worksheet.Columns[1].Locked = true;
            //worksheet.Columns[2].Locked = true;
            worksheet.Columns.Locked = true;
            worksheet.Protect("bom", missing, missing, missing, true, missing, missing,
                    missing, missing, missing, missing, missing, missing, missing, missing, missing);
            //UserInterfaceOnly: true
        }

        public void resizeColumns(Worksheet worksheet)
        {
            worksheet.Cells.EntireColumn.AutoFit();

            worksheet.Columns[4].ColumnWidth =
                worksheet.Columns[4].ColumnWidth > 80 ?
                    worksheet.Columns[4].ColumnWidth = 80 :
                    worksheet.Columns[4].ColumnWidth;
            worksheet.Cells[8, 4].Style.WrapText = true;//does not work
            worksheet.get_Range("D8").WrapText = true;

            worksheet.Columns[7].ColumnWidth =
                worksheet.Columns[7].ColumnWidth > 50 ?
                    worksheet.Columns[7].ColumnWidth = 50 :
                    worksheet.Columns[7].ColumnWidth;
            worksheet.get_Range("G11").WrapText = true;
        }

        public IList<WorkItemRecord> TimesheetRecords_DELETE(UserDataSearchTFS userData)
        {
            return Convert_TFS_Events_To_Excel_Format_DELETE(ReturnTFSEvents_ListWorkItems_DELETE(userData), userData.Month, userData.Year);
        }

        public IList<WorkItemRecord> TimesheetRecords(UserDataSearchTFS userData)
        {
            return Convert_TFS_Events_To_Excel_Format(ReturnTFSEvents_ListWorkItems(userData), userData.Month, userData.Year);
        }

        #endregion EXCEL

        private bool IsWeekend(DateTime day)
        {
            return (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday);
        }

        [HttpGet]
        public JsonResult ReadJsonHolidaysFile(string year)
        {
            return Json(DeserializeReadJsonHolidaysFile(year), JsonRequestBehavior.AllowGet);
        }

        private List<JsonHolidays> DeserializeReadJsonHolidaysFile(string year)
        {
            try
            {
                DateTime _date = DateTime.Now;
                using (StreamReader r = new StreamReader(Server.MapPath(jsonHolidaysServerPath + year + ".json")))
                {
                    string json = r.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    var result = jss.Deserialize<List<JsonHolidays>>(json);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }

}