using ClosedXML.Excel;
//using DocumentFormat.OpenXml.Bibliography;
//using DocumentFormat.OpenXml.Drawing.Charts;
//using DocumentFormat.OpenXml.Spreadsheet;
//using Microsoft.Office.Interop.Excel;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
//using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.Common;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
//using System.Runtime.InteropServices;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Models;
using TimesheetScheduler.Services;
using TimesheetScheduler.ViewModel;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
//using Application = Microsoft.Office.Interop.Excel.Application;
//using Color = System.Drawing.Color;
//
using ProjectInfo = Microsoft.TeamFoundation.Server.ProjectInfo;
//using Workbook = Microsoft.Office.Interop.Excel.Workbook;
//using Worksheet = Microsoft.Office.Interop.Excel.Worksheet;

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

            _readJsonFilesService = new ReadJsonFiles();
        }

        private static IUtilService _utilService;
        private static IReadJsonFiles _readJsonFilesService;

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

        public ActionResult TFSTaskFinder()
        {
            //ViewBag.Message = "Tracking Process.";
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

        [HttpPost]
        public JsonResult ConnectTFS(UserDataSearchTFS userData)//, int _month, int _year)
        {
            IList<object> joinWorkitems = new List<object>();
            userData.UserName = userData.UserName.Replace("'", "''");
            joinWorkitems.Add(ReturnTFSEvents_ListWorkItems(userData));
            joinWorkitems.Add(ReturnTFSEvents_ListWorkItemsWithoutStartDate(userData));
            //FormatTasksForEmailConfirmation(userData);
            //ConsolidatedReportData(userData);
            return Json(joinWorkitems, JsonRequestBehavior.AllowGet);
        }

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

        private WorkItemSerialized convertTFSObjectToWorkItemSerialized(WorkItem wi, string projectNameTFS, bool withoutStartDate = false)
        {

            var _urlTFS = GetUrlTfs();

            var _workItemsLinked = "";
            for (int i = 0; i < wi.WorkItemLinks.Count; i++)
            {
                _workItemsLinked += "#" + wi.WorkItemLinks[i].TargetId + " ";
            }
            var wiSerialized = new WorkItemSerialized();

            wiSerialized.Id = wi["Id"].ToString();
            wiSerialized.Title = wi["Title"].ToString();
            wiSerialized.WorkItemsLinked = _workItemsLinked;
            wiSerialized.State = wi.State;
            wiSerialized.LinkUrl = _urlTFS + projectNameTFS + "/_queries?id=" + wi["Id"].ToString();
            wiSerialized.CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : 0;
            wiSerialized.RemainingWork = wi["Remaining Work"] != null ? (double)wi["Remaining Work"] : 0;
            wiSerialized.CreationDate = wi.CreatedDate;

            if (withoutStartDate)
            {
                wiSerialized.Description = wi["Description"].ToString();
            }
            else
            {
                wiSerialized.StartDate = wi["Start Date"] != null ? (DateTime)wi["Start Date"] : (DateTime?)null;
                wiSerialized.Description = WebUtility.HtmlDecode(wi["Description"].ToString());
                wiSerialized.IsWeekend = wi["Start Date"] != null ? IsWeekend((DateTime)wi["Start Date"]) : (bool?)null;
            }

            return wiSerialized;
        }

        public ConsolidatedMonthUserData ReturnTFSEvents_ListWorkItems(UserDataSearchTFS userData)
        {
            try
            {
                IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();

                var _urlTFS = GetUrlTfs();

                foreach (WorkItem wi in GetTFSTaskByMonth(userData))
                {
                    listWorkItems.Add(convertTFSObjectToWorkItemSerialized(wi, userData.ProjectNameTFS, false));
                }

                return formatTFSResult(ref listWorkItems, userData.UserName, new DateTime(userData.Year, userData.Month, 1));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public IList<WorkItemSerialized> ReturnTFSEvents_ListWorkItemsWithoutStartDate(UserDataSearchTFS userData)
        {
            IList<WorkItemSerialized> listWorkItemsWithoutStartDate = new List<WorkItemSerialized>();

            foreach (WorkItem wi in GetTFSTaskByMonth(userData, true))
            {
                listWorkItemsWithoutStartDate.Add(convertTFSObjectToWorkItemSerialized(wi, userData.ProjectNameTFS, true));
            }

            return listWorkItemsWithoutStartDate.OrderByDescending(x => x.CreationDate).ToList();

        }

        private ConsolidatedMonthUserData formatTFSResult(ref IList<WorkItemSerialized> listWorkItems, string userName, DateTime period)
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

                consolidatedMonthUserData.UserName = userName;
                consolidatedMonthUserData.Period = period;
                consolidatedMonthUserData.TotalExcludingVAT = consolidatedMonthUserData.RateExcludingVAT * (decimal)consolidatedMonthUserData.WorkedDays;
                consolidatedMonthUserData.TotalIncludingVAT = consolidatedMonthUserData.TotalExcludingVAT + (consolidatedMonthUserData.TotalExcludingVAT * (_utilService.FetchVatByDate(period) / 100));

                return consolidatedMonthUserData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private WorkItemCollection GetTFSTaskByMonth(UserDataSearchTFS userData, bool startDateNull = false)
        {//GetTFSUserData
            try
            {
                Uri tfsUri = new Uri(GetUrlTfs());
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                var projectName = userData.ProjectNameTFS;//GetProjectNameTFS();
                var _iterationPath = userData.IterationPathTFS;// GetIterationPathTFS();

                string filterByDate = startDateNull ? " AND [Microsoft.VSTS.Scheduling.StartDate] = ''" :
                    (" AND [Microsoft.VSTS.Scheduling.StartDate] >= '" + $"{userData.Year}/{userData.Month}/01'" +
                     " AND [Microsoft.VSTS.Scheduling.StartDate] <= '" + $"{userData.Year}/{userData.Month}/{DateTime.DaysInMonth(userData.Year, userData.Month)}'");

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
                    filterByDate +
                    " ORDER BY [System.Id], [System.WorkItemType]");

                return WIC;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string CreateTaskOnTFS(string userName, string startDate, string title, string state, string description, string workItemLink, float chargeableHours = 7.5f, float nonchargeableHours = 0)
        {
            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();

            Uri tfsUri = new Uri(GetUrlTfs());
            try
            {
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
                newWI.Fields["Completed Work"].Value = chargeableHours;
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
            catch (Exception ex)
            {
                throw ex;
            }
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
            //var _iterationPath = GetIterationPathTFS();

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

        private WorkItemCollection GetMonthlyRatesByTeam(JsonUser userData, int Month, int Year)
        {
            try
            {
                Uri tfsUri = new Uri(GetUrlTfs());
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                var projectName = GetProjectNameTFS();
                var _iterationPath = GetIterationPathTFS();

                WorkItemCollection WIC = WIS.Query(
                    " SELECT  [System.Id]," +
                    " [Microsoft.VSTS.Scheduling.CompletedWork], " +
                    " [System.AssignedTo] " +
                    " FROM WorkItems " +
                    " WHERE [System.TeamProject] = '" + userData.ProjectNameTFS + "'" +
                    " AND [Iteration Path] = '" + userData.IterationPathTFS + "'" +
                    " AND [Assigned To] = '" + userData.Name.Replace("'", "''") + "'" +
                    " AND [Work Item Type] = 'Task'" +  //only Task -> Test Case doesn't have the same fields, causing query errors
                    " AND [Microsoft.VSTS.Scheduling.StartDate] >= '" + $"{Year}/{Month}/01'" +
                    " AND [Microsoft.VSTS.Scheduling.StartDate] <= '" + $"{Year}/{Month}/{DateTime.DaysInMonth(Year, Month)}'" +
                    " ORDER BY [Assigned To] ");

                return WIC;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public JsonResult ConsolidatedReportData(ReportRequestByUsersDTO selectedMembers)
        {
            try
            {
                return Json(consolidatedReportDataFiguresDTO(selectedMembers), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private FiguresDTO consolidatedReportDataFiguresDTO(ReportRequestByUsersDTO selectedMembers)
        {
            IList<WorkItemSerialized> listWorkItems;
            IList<ConsolidatedRateMonthly> listConsolidatedRateMonthly = new List<ConsolidatedRateMonthly>();

            foreach (var member in selectedMembers.SelectedMembers)
            {
                listWorkItems = new List<WorkItemSerialized>();

                var userWorkItemList = GetMonthlyRatesByTeam(member, selectedMembers.Month, selectedMembers.Year);

                foreach (WorkItem wi in userWorkItemList)
                {
                    listWorkItems.Add(convertTFSObjectToWorkItemSerialized(wi, member.ProjectNameTFS, false));
                }

                var period = new DateTime(selectedMembers.Year, selectedMembers.Month, 1);
                var _item = formatTFSResult(ref listWorkItems, member.Name, period);
                
                listConsolidatedRateMonthly.Add(new ConsolidatedRateMonthly()
                {
                    ChargeableHours = _item.ChargeableHours,
                    MemberName = member.Name,
                    DaysWorked = _item.WorkedDays,
                    RateExcVat = _item.RateExcludingVAT,
                    RateIncVat = _item.RateIncludingVAT,
                    DayRateExcVat = member.Chargeable ? _item.TotalExcludingVAT : 0,
                    DayRateIncVat = member.Chargeable ? _item.TotalIncludingVAT : 0,
                    TeamDivision = member.TeamDivision,
                    Chargeable = member.Chargeable,
                    Role = member.Role,
                    ProjectNameTFS = _readJsonFilesService.GetTeamNameByProjectId(member.ProjectId) //member.ProjectNameTFS
                });
            }

            var _period = new DateTime(selectedMembers.Year, selectedMembers.Month, 1);
            FiguresDTO _figures = new FiguresDTO();
            _figures.Members = listConsolidatedRateMonthly;
            _figures.TeamName = listConsolidatedRateMonthly.FirstOrDefault().ProjectNameTFS;
            _figures.VatApplied = _utilService.FetchVatTextByDate(_period);
            _figures.PeriodSearched = _period;

            foreach (var item in listConsolidatedRateMonthly.GroupBy(x => x.TeamDivision))
            {
                var _currentFigure = new FiguresByTeamDivisionDTO() { TeamDivision = item.Key };

                _currentFigure.TotalExclVAT = item.Where(x => x.Chargeable).Sum(x => x.DayRateExcVat).ToString();
                _currentFigure.TotalInclVAT = item.Where(x => x.Chargeable).Sum(x => x.DayRateIncVat).ToString();

                //_currentFigure.FiguresIndexes.Add(new FiguresIndexesDTO()
                //{
                //    //Label = "Total Excl VAT:",//it has to be refactor (not need anymore to have labels being set here)
                //    Value = item.Where(x=>x.Chargeable).Sum(x => x.DayRateExcVat).ToString()
                //});
                //_currentFigure.FiguresIndexes.Add(new FiguresIndexesDTO()
                //{
                //    //Label = "Total Incl VAT:",
                //    Value = item.Where(x => x.Chargeable).Sum(x => x.DayRateIncVat).ToString()
                //});

                _figures.FiguresByTeamDivision.Add(_currentFigure);
            }

            _figures.TotalExclVat = listConsolidatedRateMonthly.Where(x => x.Chargeable).Sum(x => x.DayRateExcVat).ToString();
            _figures.TotalInclVat = listConsolidatedRateMonthly.Where(x => x.Chargeable).Sum(x => x.DayRateIncVat).ToString();
            return _figures;
        }

        [HttpGet]
        public string FindTFSTask(int workItemId)
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
                //throw new Exception("The work item does not exist, or you do not have permission to access it. (" + workItemId + ")");
                throw new Exception(ex.Message + " - (" + workItemId + ")");
            }

            return _urlTFS + workItem.Project.Name + "/_queries?id=" + workItem["Id"].ToString();
        }

        #endregion TFS

        #region EXCEL

        #region UTIL

        public string NameSplittedByUnderscore(string name, string separator)
        {
            return name.Replace(" ", separator);
        }

        [HttpGet]
        public string TimesheetFileName(string userName, char separator, int _month, int _year)
        {
            var _monthFormatted = new DateTime(_year, _month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            var _fileName = "Timesheet" + separator + NameSplittedByUnderscore(userName, separator.ToString()) + separator + _monthFormatted + separator + _year;
            return _fileName;
        }

        [HttpGet]
        public bool PathExists(string path)
        {
            return Directory.Exists(path.ToString());
        }

        [HttpGet]
        public string GetPathAndFTimesheetFileName(string path, string userName, int _month, int _year)
        {
            if (PathExists(path))
            {
                return path + TimesheetFileName(userName.Replace("'", ""), '_', _month, _year);
            }
            throw new Exception("Path doesn't exist or you don't have access");
        }

        [HttpGet]
        public string TimesheetSaveLocation()
        {
            var _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";
            var _path = ConfigurationManager.AppSettings["pathTimesheet"];
            return _path == null ? _desktop : (PathExists(_path.ToString()) ? _path.ToString() : _desktop);
        }

        [HttpGet]
        public string TimesheetSaveLocationAndFileName(string userName, int _month, int _year)
        {
            return TimesheetSaveLocation() + TimesheetFileName(userName.Replace("'", ""), '_', _month, _year);
        }

        //public void SetCellValue(Worksheet worksheet, CellObject cellObj)
        //{
        //    worksheet.get_Range(cellObj.CellPosition).Value = cellObj.CellValue;
        //}

        public void SetCellValueClosedXML(IXLWorksheet ws, CellObjectClosedXML cellObj)
        {
            ws.Cell(cellObj.CellPosition).Value = cellObj.CellValue;
        }

        //public void FormatCell(Worksheet worksheet, CellObject cellObj)
        //{
        //    worksheet.get_Range(cellObj.CellPosition).Font.Size = cellObj.FormatParams.fontSize;
        //    worksheet.get_Range(cellObj.CellPosition).Font.Bold = cellObj.FormatParams.fontBold;
        //    worksheet.get_Range(cellObj.CellPosition).Font.Italic = cellObj.FormatParams.fontItalic;
        //    worksheet.get_Range(cellObj.CellPosition).Font.Color = cellObj.FormatParams.fontColor;
        //    worksheet.get_Range(cellObj.CellPosition).HorizontalAlignment = cellObj.FormatParams.aligment;
        //    worksheet.get_Range(cellObj.CellPosition).Locked = cellObj.FormatParams.lockCell;
        //    //worksheet.get_Range(cellObj.CellPosition).Interior.Color = cellObj.FormatParams.cellBackgroundColor;
        //}

        public void FormatCellClosedXML(IXLWorksheet ws, CellObjectClosedXML cellObj)
        {
            var cellKey = cellObj.CellPosition;
            ws.Cell(cellKey).Value = cellObj.CellValue;
            ws.Cell(cellKey).FormulaA1 = cellObj.CellFormula;
            ws.Cell(cellKey).Style.Font.FontSize = cellObj.FormatParams.fontSize;
            ws.Cell(cellKey).Style.Font.Bold = cellObj.FormatParams.fontBold;
            ws.Cell(cellKey).Style.Font.Italic = cellObj.FormatParams.fontItalic;
            ws.Cell(cellKey).Style.Font.FontColor = cellObj.FormatParams.fontColor;
            //ws.Cell(cellKey).Style.Fill.SetBackgroundColor(cellObj.FormatParams.cellBackgroundColor);
            ws.Cell(cellKey).Style.Alignment.SetVertical(cellObj.FormatParams.aligmentVertical);// XLAlignmentVerticalValues.Center);
            ws.Cell(cellKey).Style.Alignment.SetHorizontal(cellObj.FormatParams.aligmentHorizontal);//XLAlignmentHorizontalValues.Left);
            ws.Cell(cellKey).Style.Protection.SetLocked(cellObj.FormatParams.lockCell);
            //workSheet.Range(startRow, startColumn, endRow, endColumn).Style.Protection.SetLocked(true);
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

        #region CellNames
        public CellObjectClosedXML CellProjectNameLabelClosedXML { get; set; }
        public CellObjectClosedXML CellProjectNameInputClosedXML { get; set; }

        //Period
        public CellObjectClosedXML CellPeriodLabelClosedXML { get; set; }
        public CellObjectClosedXML CellPeriodInputClosedXML { get; set; }

        //NAME
        public CellObjectClosedXML CellNameLabelClosedXML { get; set; }
        public CellObjectClosedXML CellNameInputClosedXML { get; set; }

        //Total working days in month
        public CellObjectClosedXML CellTotalWorkingDaysInMonthLabelClosedXML { get; set; }
        public CellObjectClosedXML CellTotalWorkingDaysInMonthInputClosedXML { get; set; }

        //Total hours
        public CellObjectClosedXML CellTotalHoursLabelClosedXML { get; set; }
        public CellObjectClosedXML CellTotalHoursInputClosedXML { get; set; }

        //Total Chargeable hours
        public CellObjectClosedXML CellTotalChargeableHoursLabelClosedXML { get; set; }
        public CellObjectClosedXML CellTotalChargeableHoursInputClosedXML { get; set; }

        //Total Non-Chargeable hours
        public CellObjectClosedXML CellTotalNonChargeableHoursLabelClosedXML { get; set; }
        public CellObjectClosedXML CellTotalNonChargeableHoursInputClosedXML { get; set; }

        // --------------------------------------- END HEADER TIMESHEET -----------------------------------------

        //---------------------------------------- INIT HEADER TABLE---------------------------------------------

        //Header Table Id
        public CellObjectClosedXML CellHeaderTableIdClosedXML { get; set; }

        //Header Table Date
        public CellObjectClosedXML CellHeaderTableDateClosedXML { get; set; }

        //Header Table WorkItem
        public CellObjectClosedXML CellHeaderTableWorkItemClosedXML { get; set; }

        //Header Table Description
        public CellObjectClosedXML CellHeaderTableDescriptionClosedXML { get; set; }

        //Header Table Chargeable Hours
        public CellObjectClosedXML CellHeaderTableChargeableHoursClosedXML { get; set; }

        //Header Table Non-Chargeable Hours
        public CellObjectClosedXML CellHeaderTableNonChargeableHoursClosedXML { get; set; }

        //Header Table Comments
        public CellObjectClosedXML CellHeaderTableCommentsClosedXML { get; set; }
        #endregion CellNames

        //---------------------------------------- END HEADER TABLE---------------------------------------------

        #endregion Const Cell Range and Labels

        //public void InitExcelVariables(string userName, int _month, int _year)
        //{
        //    UserName = userName;
        //    MonthTimesheet = new DateTime(_year, _month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
        //    YearTimesheet = _year.ToString();

        //    CellProjectNameLabel = new CellObject
        //    {
        //        CellPosition = "B1",
        //        CellValue = "Project Name",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellProjectNameInput = new CellObject
        //    {
        //        CellPosition = "C1",
        //        CellValue = "BOMi Modelling Team", //TODO DYNAMIC
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = false,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellPeriodLabel = new CellObject
        //    {
        //        CellPosition = "B2",
        //        CellValue = "Period",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellPeriodInput = new CellObject
        //    {
        //        CellPosition = "C2",
        //        CellValue = "'" + new DateTime(_year, _month, 1).ToString("MMM/yyyy", CultureInfo.CreateSpecificCulture("en")), //TODO DYNAMIC
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = false,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellNameLabel = new CellObject
        //    {
        //        CellPosition = "B3",
        //        CellValue = "Name",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellNameInput = new CellObject
        //    {
        //        CellPosition = "C3",
        //        CellValue = UserName, //TODO DYNAMIC
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = false,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignLeft,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalWorkingDaysInMonthLabel = new CellObject
        //    {
        //        CellPosition = "D1",
        //        CellValue = "Total Working Days In Month",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalWorkingDaysInMonthInput = new CellObject
        //    {
        //        CellPosition = "E1",
        //        CellValue = "=E3/7.5", //TODO: apply formula
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalHoursLabel = new CellObject
        //    {
        //        CellPosition = "D2",
        //        CellValue = "Total Hours",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalHoursInput = new CellObject
        //    {
        //        CellPosition = "E2",
        //        CellValue = "=SUM(E3:E4)", //TODO: apply formula
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalChargeableHoursLabel = new CellObject
        //    {
        //        CellPosition = "D3",
        //        CellValue = "Total Chargeable Hours",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalChargeableHoursInput = new CellObject
        //    {
        //        CellPosition = "E3",
        //        //CellValue = "=SUM(E6:E50)", //TODO: apply formula // it gets set in CreateTable method (868)
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalNonChargeableHoursLabel = new CellObject
        //    {
        //        CellPosition = "D4",
        //        CellValue = "Total Non-Chargeable Hours",
        //        //(Hours worked in excess of agreed daily working hours or non chargeable days as agreed)
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellTotalNonChargeableHoursInput = new CellObject
        //    {
        //        CellPosition = "E4",
        //        //CellValue = "=SUM(F6:F50)", //TODO: apply formula // it gets set in CreateTable method (868)
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 11,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignRight,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableId = new CellObject
        //    {
        //        CellPosition = "A5",
        //        CellValue = "Id",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableDate = new CellObject
        //    {
        //        CellPosition = "B5",
        //        CellValue = "Date",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableWorkItem = new CellObject
        //    {
        //        CellPosition = "C5",
        //        CellValue = "Work Item",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableDescription = new CellObject
        //    {
        //        CellPosition = "D5",
        //        CellValue = "Title",//"Description",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableChargeableHours = new CellObject
        //    {
        //        CellPosition = "E5",
        //        CellValue = "Chargeable Hours",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableNonChargeableHours = new CellObject
        //    {
        //        CellPosition = "F5",
        //        CellValue = "Non-Charg. Hours", //"Non-Chargeable Hours",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };

        //    CellHeaderTableComments = new CellObject
        //    {
        //        CellPosition = "G5",
        //        CellValue = "Work Items Linked", //"Comments",
        //        FormatParams = new ParamsFormatCell()
        //        {
        //            fontSize = 12,
        //            fontBold = true,
        //            fontItalic = false,
        //            fontColor = Color.Black,
        //            cellBackgroundColor = Color.Transparent,
        //            aligment = XlHAlign.xlHAlignCenter,
        //            lockCell = true
        //        }
        //    };
        //}

        public void InitExcelVariablesClosedXML(string userName, int _month, int _year)
        {
            UserName = userName;
            MonthTimesheet = new DateTime(_year, _month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            YearTimesheet = _year.ToString();

            CellProjectNameLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "B1",
                CellValue = "Project Name",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true
                }
            };

            CellProjectNameInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "C1",
                CellValue = "BOMi Modelling Team", //TODO DYNAMIC
                FormatParams = new ParamsFormatCellClosedXML()
            };

            CellPeriodLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "B2",
                CellValue = "Period",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true
                }
            };

            CellPeriodInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "C2",
                CellValue = "'" + new DateTime(_year, _month, 1).ToString("MMM/yyyy", CultureInfo.CreateSpecificCulture("en")), //TODO DYNAMIC
                FormatParams = new ParamsFormatCellClosedXML()
            };

            CellNameLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "B3",
                CellValue = "Name",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true
                }
            };

            CellNameInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "C3",
                CellValue = UserName, //TODO DYNAMIC
                FormatParams = new ParamsFormatCellClosedXML()
            };

            CellTotalWorkingDaysInMonthLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "D1",
                CellValue = "Total Working Days In Month",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalWorkingDaysInMonthInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "E1",
                //CellValue = "=E3/7.5", //TODO: apply formula
                CellFormula = "=E3/7.5", //TODO: apply formula
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalHoursLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "D2",
                CellValue = "Total Hours",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalHoursInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "E2",
                //CellValue = "=SUM(E3:E4)", //TODO: apply formula
                CellFormula = "=SUM(E3:E4)", //TODO: apply formula
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalChargeableHoursLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "D3",
                CellValue = "Total Chargeable Hours",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalChargeableHoursInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "E3",
                //CellValue = "=SUM(E6:E50)", //TODO: apply formula // it gets set in CreateTable method (868)
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalNonChargeableHoursLabelClosedXML = new CellObjectClosedXML
            {
                CellPosition = "D4",
                CellValue = "Total Non-Chargeable Hours",
                //(Hours worked in excess of agreed daily working hours or non chargeable days as agreed)
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellTotalNonChargeableHoursInputClosedXML = new CellObjectClosedXML
            {
                CellPosition = "E4",
                //CellValue = "=SUM(F6:F50)", //TODO: apply formula // it gets set in CreateTable method (868)
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Right
                }
            };

            CellHeaderTableIdClosedXML = new CellObjectClosedXML
            {
                CellPosition = "A5",
                CellValue = "Id",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableDateClosedXML = new CellObjectClosedXML
            {
                CellPosition = "B5",
                CellValue = "Date",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableWorkItemClosedXML = new CellObjectClosedXML
            {
                CellPosition = "C5",
                CellValue = "Work Item",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableDescriptionClosedXML = new CellObjectClosedXML
            {
                CellPosition = "D5",
                CellValue = "Title",//"Description",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableChargeableHoursClosedXML = new CellObjectClosedXML
            {
                CellPosition = "E5",
                CellValue = "Chargeable Hours",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableNonChargeableHoursClosedXML = new CellObjectClosedXML
            {
                CellPosition = "F5",
                CellValue = "Non-Charg. Hours", //"Non-Chargeable Hours",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };

            CellHeaderTableCommentsClosedXML = new CellObjectClosedXML
            {
                CellPosition = "G5",
                CellValue = "Work Items Linked", //"Comments",
                FormatParams = new ParamsFormatCellClosedXML()
                {
                    fontSize = 12,
                    fontBold = true,
                    aligmentHorizontal = XLAlignmentHorizontalValues.Center
                }
            };
        }

        //private void CreateHeader(Worksheet worksheet)
        //{
        //    //-------------------------------- INIT TIMESHEET HEADER -----------------------------------------------
        //    SetValAndFormatCell(worksheet, CellProjectNameLabel);//B1 - Project Name
        //    SetValAndFormatCell(worksheet, CellProjectNameInput);//C1 - Project Name
        //    SetValAndFormatCell(worksheet, CellPeriodLabel);//B2 - Month
        //    SetValAndFormatCell(worksheet, CellPeriodInput);//C2 - Month
        //    SetValAndFormatCell(worksheet, CellNameLabel);//B3 - Name
        //    SetValAndFormatCell(worksheet, CellNameInput);//C3 - Name
        //    //----------------------------------------------------------------------------------------------
        //    SetValAndFormatCell(worksheet, CellTotalWorkingDaysInMonthLabel);//D1 - Total working days in month
        //    SetValAndFormatCell(worksheet, CellTotalWorkingDaysInMonthInput);//E1 - Total working days in month
        //    SetValAndFormatCell(worksheet, CellTotalHoursLabel);//D2 - Total Hours
        //    SetValAndFormatCell(worksheet, CellTotalHoursInput);//E2 - Total Hours
        //    SetValAndFormatCell(worksheet, CellTotalChargeableHoursLabel);//D3 - Total Chargeable Hours
        //    SetValAndFormatCell(worksheet, CellTotalChargeableHoursInput);//E3 - Total Chargeable Hours
        //    SetValAndFormatCell(worksheet, CellTotalNonChargeableHoursLabel);//D4 - Total Non-Chargeable Hours
        //    SetValAndFormatCell(worksheet, CellTotalNonChargeableHoursInput);//E4 - Total Non-Chargeable Hours
        //    //-------------------------------- END TIMESHEET HEADER -----------------------------------------------

        //    //---------------------------------------- INIT HEADER TABLE---------------------------------------------
        //    SetValAndFormatCell(worksheet, CellHeaderTableId);
        //    SetValAndFormatCell(worksheet, CellHeaderTableDate);
        //    SetValAndFormatCell(worksheet, CellHeaderTableWorkItem);
        //    SetValAndFormatCell(worksheet, CellHeaderTableDescription);
        //    SetValAndFormatCell(worksheet, CellHeaderTableChargeableHours);
        //    SetValAndFormatCell(worksheet, CellHeaderTableNonChargeableHours);
        //    SetValAndFormatCell(worksheet, CellHeaderTableComments);
        //    //---------------------------------------- END HEADER TABLE---------------------------------------------
        //}

        private void CreateHeaderClosedXML(IXLWorksheet ws)
        {
            //-------------------------------- INIT TIMESHEET HEADER -----------------------------------------------
            SetValAndFormatCellClosedXML(ws, CellProjectNameLabelClosedXML);//B1 - Project Name
            SetValAndFormatCellClosedXML(ws, CellProjectNameInputClosedXML);//C1 - Project Name
            SetValAndFormatCellClosedXML(ws, CellPeriodLabelClosedXML);//B2 - Month
            SetValAndFormatCellClosedXML(ws, CellPeriodInputClosedXML);//C2 - Month
            SetValAndFormatCellClosedXML(ws, CellNameLabelClosedXML);//B3 - Name
            SetValAndFormatCellClosedXML(ws, CellNameInputClosedXML);//C3 - Name
            //----------------------------------------------------------------------------------------------
            SetValAndFormatCellClosedXML(ws, CellTotalWorkingDaysInMonthLabelClosedXML);//D1 - Total working days in month
            SetValAndFormatCellClosedXML(ws, CellTotalWorkingDaysInMonthInputClosedXML);//E1 - Total working days in month
            SetValAndFormatCellClosedXML(ws, CellTotalHoursLabelClosedXML);//D2 - Total Hours
            SetValAndFormatCellClosedXML(ws, CellTotalHoursInputClosedXML);//E2 - Total Hours
            SetValAndFormatCellClosedXML(ws, CellTotalChargeableHoursLabelClosedXML);//D3 - Total Chargeable Hours
            SetValAndFormatCellClosedXML(ws, CellTotalChargeableHoursInputClosedXML);//E3 - Total Chargeable Hours
            SetValAndFormatCellClosedXML(ws, CellTotalNonChargeableHoursLabelClosedXML);//D4 - Total Non-Chargeable Hours
            SetValAndFormatCellClosedXML(ws, CellTotalNonChargeableHoursInputClosedXML);//E4 - Total Non-Chargeable Hours
            //-------------------------------- END TIMESHEET HEADER -----------------------------------------------

            //---------------------------------------- INIT HEADER TABLE---------------------------------------------
            SetValAndFormatCellClosedXML(ws, CellHeaderTableIdClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableDateClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableWorkItemClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableDescriptionClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableChargeableHoursClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableNonChargeableHoursClosedXML);
            SetValAndFormatCellClosedXML(ws, CellHeaderTableCommentsClosedXML);
            //---------------------------------------- END HEADER TABLE---------------------------------------------
        }

        //private int CreateTable(Worksheet worksheet, UserDataSearchTFS userData)
        //{
        //    var _table = TimesheetRecords(userData);
        //    int firstRow = 6;
        //    int lastRow = _table.Count + firstRow;
        //    int j;

        //    worksheet.get_Range(CellTotalChargeableHoursInput.CellPosition).Value = "=SUM(E" + firstRow + ":E" + lastRow + ")";
        //    worksheet.get_Range(CellTotalNonChargeableHoursInput.CellPosition).Value = "=SUM(F" + firstRow + ":F" + lastRow + ")";

        //    for (int i = firstRow; i < lastRow; i++) //ROW
        //    {
        //        j = i - firstRow;
        //        worksheet.Cells[i, 1] = _table[j].Id;
        //        worksheet.Cells[i, 2] = _table[j].Date.ToShortDateString();
        //        worksheet.Cells[i, 3] = _table[j].WorkItemNumber;
        //        worksheet.Cells[i, 4] = _table[j].Description;
        //        worksheet.Cells[i, 5] = _table[j].ChargeableHours;
        //        worksheet.Cells[i, 6] = _table[j].NonChargeableHours;
        //        worksheet.Cells[i, 7] = _table[j].Comments;

        //        worksheet.Cells[i, 2].NumberFormat = "MM/DD/YYYY"; //date in american format
        //        worksheet.Cells[i, 3].NumberFormat = "0"; //workitem formatted as number

        //        if (_table[j].IsWeekend)
        //        {
        //            //worksheet.Cells[i, 3] = ""; //WorkItemNumber
        //            //worksheet.Cells[i, 5] = ""; //ChargeableHours
        //            //worksheet.Cells[i, 6] = ""; //NonChargeableHours
        //            worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.DarkGray; //TODO
        //            worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = true;
        //        }
        //        else
        //        {
        //            worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.White; //TODO
        //            worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = false;
        //        }
        //    }

        //    worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].HorizontalAlignment = XlHAlign.xlHAlignCenter;
        //    worksheet.Range[worksheet.Cells[firstRow, 1], worksheet.Cells[lastRow, 7]].VerticalAlignment = XlHAlign.xlHAlignCenter;

        //    worksheet.Cells[1, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Working Days In Month)
        //    worksheet.Cells[2, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Hours)
        //    worksheet.Cells[3, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Chargeable Hours)
        //    worksheet.Cells[4, 5].NumberFormat = "#,##0.00"; //header formula with 2 decimal places (Total Non-Chargeable Hours)

        //    return _table.Count;
        //}

        private int CreateTableClosedXML(IXLWorksheet ws, UserDataSearchTFS userData)
        {
            var _table = TimesheetRecords(userData);
            int firstRow = 6;
            int lastRow = _table.Count + firstRow;
            int j;

            ws.Cell(CellTotalChargeableHoursInputClosedXML.CellPosition).FormulaA1 = "=SUM(E" + firstRow + ":E" + lastRow + ")";
            ws.Cell(CellTotalNonChargeableHoursInputClosedXML.CellPosition).FormulaA1 = "=SUM(F" + firstRow + ":F" + lastRow + ")";

            for (int i = firstRow; i < lastRow; i++) //ROW
            {
                j = i - firstRow;

                ws.Cell(i, 1).Value = _table[j].Id;
                ws.Cell(i, 2).Value = _table[j].Date.ToShortDateString();
                ws.Cell(i, 3).Value = _table[j].WorkItemNumber;
                ws.Cell(i, 4).Value = _table[j].Description;
                ws.Cell(i, 5).Value = _table[j].ChargeableHours;
                ws.Cell(i, 6).Value = _table[j].NonChargeableHours;
                ws.Cell(i, 7).Value = _table[j].Comments;

                ws.Cell(i, 2).Style.NumberFormat.Format = "MM/DD/YYYY"; //date in american format;
                ws.Cell(i, 3).Style.NumberFormat.Format = "0"; //date in american format;

                if (_table[j].IsWeekend)
                {
                    ws.Range(ws.Cell(i, 1), ws.Cell(i, 7)).Style.Fill.SetBackgroundColor(XLColor.DarkGray);
                    ws.Range(ws.Cell(i, 1), ws.Cell(i, 7)).Style.Protection.SetLocked(true);
                }
                else
                {
                    ws.Range(ws.Cell(i, 1), ws.Cell(i, 7)).Style.Fill.SetBackgroundColor(XLColor.White);
                }

                ws.Range(ws.Cell(i, 1), ws.Cell(i, 7)).Style.Protection.SetLocked(true);
            }

            ws.Range(ws.Cell(firstRow, 1), ws.Cell(lastRow, 7)).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(ws.Cell(firstRow, 1), ws.Cell(lastRow, 7)).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(1, 5).Style.NumberFormat.Format = "#,##0.00"; //header formula with 2 decimal places (Total Working Days In Month)
            ws.Cell(2, 5).Style.NumberFormat.Format = "#,##0.00"; //header formula with 2 decimal places (Total Hours)
            ws.Cell(3, 5).Style.NumberFormat.Format = "#,##0.00"; //header formula with 2 decimal places (Total Chargeable Hours)
            ws.Cell(4, 5).Style.NumberFormat.Format = "#,##0.00"; //header formula with 2 decimal places (Total Non-Chargeable Hours)

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
                            _currentNonChargeableHours = item.RemainingWork.HasValue ? item.RemainingWork.Value : 0;
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

        //public void SetValAndFormatCell(Worksheet worksheet, CellObject cellObj)
        //{
        //    SetCellValue(worksheet, cellObj);
        //    FormatCell(worksheet, cellObj);
        //}

        public void SetValAndFormatCellClosedXML(IXLWorksheet worksheet, CellObjectClosedXML cellObj)
        {
            SetCellValueClosedXML(worksheet, cellObj);
            FormatCellClosedXML(worksheet, cellObj);
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

        //[HttpPost]
        //public string SaveExcelFile_Old(UserDataSearchTFS userData)
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
        //        //worksheet.Name = "Timesheet_" + userData.UserName;//.Replace(" ", "_");

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
        //        return "Internal Code (1) - " + ex.Message;
        //    }
        //    finally
        //    {
        //        worksheet = null;
        //        celLrangE = null;
        //        worKbooK = null;

        //    }
        //}

        [HttpPost]
        public string SaveExcelFile(UserDataSearchTFS userData, string folderPath = null)
        {
            try
            {
                StringBuilder result = new StringBuilder();

                userData.UserName = userData.UserName.Replace("'", "''");

                var workBookClosedXML = new XLWorkbook();
                var _worksheet = workBookClosedXML.Worksheets.Add("Timesheet");
                InitExcelVariablesClosedXML(userData.UserName, userData.Month, userData.Year);

                CreateHeaderClosedXML(_worksheet);

                var tableEventCount = 0;
                tableEventCount = CreateTableClosedXML(_worksheet, userData);

                resizeColumnsClosedXML(_worksheet);

                formatBordersClosedXML(_worksheet, tableEventCount);

                protectSheetClosedXML(_worksheet);

                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = TimesheetSaveLocationAndFileName(userData.UserName, userData.Month, userData.Year);
                }
                //else
                //{
                //    folderPath += TimesheetFileName(userData.UserName, '_', userData.Month, userData.Year);
                //}

                workBookClosedXML.SaveAs(folderPath + ".xlsx");

                var timesheetFileName = folderPath.Split(new string[] { "\\\\" }, StringSplitOptions.None).LastOrDefault();

                result.Append($"File saved sucessfully!({timesheetFileName})");
                result.Append("<br />");
                return result.ToString();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return "Internal Code (1) - " + ex.Message;
            }
        }

        //[HttpPost]
        //public string BulkSaveExcelFile_Old(IList<UserDataSearchTFS> userData)
        //{
        //    Application excel = null;
        //    Workbook worKbooK = null;
        //    Worksheet worksheet = null;
        //    Range celLrangE = null;
        //    StringBuilder result = new StringBuilder();
        //    foreach (var item in userData)
        //    {
        //        try
        //        {

        //            item.UserName = item.UserName.Replace("'", "''");

        //            excel = new Application();
        //            excel.Visible = true;
        //            excel.DisplayAlerts = true;
        //            worKbooK = excel.Workbooks.Add(Type.Missing);

        //            var tableEventCount = 0;

        //            InitExcelVariables(item.UserName, item.Month, item.Year);


        //            Microsoft.Office.Interop.Excel._Worksheet workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excel.ActiveSheet;
        //            worksheet = (Worksheet)worKbooK.ActiveSheet;
        //            //worksheet.Name = "Timesheet_" + item.UserName;//.Replace(" ", "_");
        //            //RPC Failed - The remote procedure call failed
        //            //https://copyprogramming.com/howto/remote-procedure-call-rpc-errors-only-thrown-when-a-user-is-connected-via-remote-desktop

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

        //            var timesheetFileName = saveParams.Split(new string[] { "\\\\" }, StringSplitOptions.None).LastOrDefault();

        //            result.Append($"File saved sucessfully!({timesheetFileName})"); // - Path: " + saveParams;
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
        //            result.AppendLine($"Internal Code (1) - {ex.Message}");
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
        public string BulkSaveExcelFile(IList<UserDataSearchTFS> userData, string folderPath = null)
        {
            StringBuilder result = new StringBuilder();
            foreach (var item in userData)
            {
                try
                {
                    var fullFolderAndFilename = folderPath + TimesheetFileName(item.UserName, '_', item.Month, item.Year);
                    result.Append(SaveExcelFile(item, fullFolderAndFilename));
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    result.AppendLine($"Internal Code (1) - {ex.Message}");
                    result.Append("<br />");
                }
            }
            return result.ToString();
        }

        //--------------------------------------------------------------------------------------------------------

        //private void WorkbookSheetChange(Workbook workbook)
        //{
        //    workbook.SheetChange += new
        //        WorkbookEvents_SheetChangeEventHandler(
        //        ThisWorkbook_SheetChange);
        //}

        //void ThisWorkbook_SheetChange(object Sh, Range Target)
        //{
        //    Worksheet sheet = (Worksheet)Sh;

        //    string changedRange = Target.get_Address(
        //        XlReferenceStyle.xlA1);
        //}

        //--------------------------------------------------------------------------------------------------------

        //private void protectSheet(Worksheet worksheet)
        //{
        //    var missing = Type.Missing;
        //    //worksheet.Columns[1].Locked = true;
        //    //worksheet.Columns[2].Locked = true;
        //    worksheet.Columns.Locked = true;
        //    worksheet.Protect("bom", missing, missing, missing, true, missing, missing,
        //            missing, missing, missing, missing, missing, missing, missing, missing, missing);
        //    //UserInterfaceOnly: true
        //}

        private void formatBordersClosedXML(IXLWorksheet _worksheet, int tableCountRow)
        {
            var borderStartsRow = 5; //first 4 rows = header
            var borderEndsRow = borderStartsRow + tableCountRow;
            var borderRange = _worksheet.Range(_worksheet.Cell(borderStartsRow, 1), _worksheet.Cell(borderEndsRow, 7));

            borderRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            borderRange.Style.Border.BottomBorderColor = XLColor.Black;

            borderRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            borderRange.Style.Border.TopBorderColor = XLColor.Black;

            borderRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            borderRange.Style.Border.LeftBorderColor = XLColor.Black;

            borderRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            borderRange.Style.Border.RightBorderColor = XLColor.Black;
        }


        private void protectSheetClosedXML(IXLWorksheet worksheet)
        {
            foreach (var item in worksheet.CellsUsed())
            {
                item.Style.Protection.SetLocked(true);
            }
            worksheet.Protect("bom");
        }

        //public void resizeColumns(Worksheet worksheet)
        //{
        //    worksheet.Cells.EntireColumn.AutoFit();

        //    worksheet.Columns[4].ColumnWidth =
        //        worksheet.Columns[4].ColumnWidth > 80 ?
        //            worksheet.Columns[4].ColumnWidth = 80 :
        //            worksheet.Columns[4].ColumnWidth;
        //    worksheet.Cells[8, 4].Style.WrapText = true;//does not work
        //    worksheet.get_Range("D8").WrapText = true;

        //    worksheet.Columns[7].ColumnWidth =
        //        worksheet.Columns[7].ColumnWidth > 50 ?
        //            worksheet.Columns[7].ColumnWidth = 50 :
        //            worksheet.Columns[7].ColumnWidth;
        //    worksheet.get_Range("G11").WrapText = true;
        //}

        public void resizeColumnsClosedXML(IXLWorksheet ws)
        {
            //worksheet.Cells.EntireColumn.AutoFit();
            ws.Columns().AdjustToContents();

            //worksheet.Columns[4].ColumnWidth =
            //    worksheet.Columns[4].ColumnWidth > 80 ?
            //        worksheet.Columns[4].ColumnWidth = 80 :
            //        worksheet.Columns[4].ColumnWidth;
            //worksheet.Cells[8, 4].Style.WrapText = true;//does not work
            //worksheet.get_Range("D8").WrapText = true;

            //worksheet.Columns[7].ColumnWidth =
            //    worksheet.Columns[7].ColumnWidth > 50 ?
            //        worksheet.Columns[7].ColumnWidth = 50 :
            //        worksheet.Columns[7].ColumnWidth;
            //worksheet.get_Range("G11").WrapText = true;
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

        //[HttpGet]
        //public void CaptureImage(bool activeScreenOnly)
        //{
        //    if (activeScreenOnly)
        //    {
        //        _utilService.CaptureActiveScreen();
        //    }
        //    else
        //    {
        //        _utilService.CaptureWholeScreen();
        //    }
        //}

        #region Create Word Doc 
        [HttpPost]
        public JsonResult CreateWordDoc(ReportRequestByUsersDTO selectedMembers)
        {

            var _data = consolidatedReportDataFiguresDTO(selectedMembers);
            var coreTeam = _data.Members.Where(x => x.TeamDivision.Equals("Core")).OrderByDescending(x=>x.RateExcVat).ToList();
            var drawdownTeam = _data.Members.Where(x => x.TeamDivision.Equals("Drawdown")).ToList();
            IList<string> headerMemberTable = new List<string>() { "Team Member", "Days", "Hours", "Rate", "Role", "Excl.VAT" };
           

            Document doc = new Document();
            Section section = doc.AddSection();

            #region VAT Label
            var vatLabel = section.AddParagraph().AppendText($"\nVAT Applied [{_data.VatApplied}]\n");
            ApplyStyleText(ref vatLabel, Color.Red);
            #endregion VAT Label

            #region Core Summary Table
            Table tableCoreSummary = section.AddTable(true);
            tableCoreSummary.Title = "Core Summary Table";
            var _excVAT = _data.FiguresByTeamDivision.FirstOrDefault(x => x.TeamDivision.Equals("Core")).TotalExclVAT;
            var _incVAT = _data.FiguresByTeamDivision.FirstOrDefault(x => x.TeamDivision.Equals("Core")).TotalInclVAT;
            CreateSummaryTable(ref doc, ref tableCoreSummary, _data.PeriodSearched.ToString("yyyy MMMM"), _excVAT, _incVAT, true);
            #endregion Core Summary Table

            section.AddParagraph().AppendText("\n");

            #region Drawdown Summary Table
            Table tableDradownSummary = section.AddTable(true);
            tableDradownSummary.Title = "Drawdown Summary Table";
            _excVAT = _data.FiguresByTeamDivision.FirstOrDefault(x => x.TeamDivision.Equals("Drawdown")).TotalExclVAT;
            _incVAT = _data.FiguresByTeamDivision.FirstOrDefault(x => x.TeamDivision.Equals("Drawdown")).TotalInclVAT;
            CreateSummaryTable(ref doc, ref tableDradownSummary, _data.PeriodSearched.ToString("yyyy MMMM"), _excVAT, _incVAT, false);
            #endregion Drawdown Summary Table

            #region Core Member Table
            var tableCoreTitle = section.AddParagraph().AppendText($"\nCore Team {_data.PeriodSearchedFullDate()}:\n");
            ApplyStyleText(ref tableCoreTitle, null);
            Table tableCoreTeam = section.AddTable(true);
            tableCoreTeam.Title = "Core Team";
            CreateMemberTable(ref doc, ref tableCoreTeam, headerMemberTable, coreTeam);
            #endregion Core Member Table

            #region Drawdown Member Table
            var tableDrawdownTitle = section.AddParagraph().AppendText($"\nDrawdown Team billed separately from {_data.PeriodSearchedFullDate()}:\n");
            ApplyStyleText(ref tableDrawdownTitle, null);
            Table dradownTeam = section.AddTable(true);
            dradownTeam.Title = "Drawdown Team";
            CreateMemberTable(ref doc, ref dradownTeam, headerMemberTable, drawdownTeam);
            #endregion Drawdown Member Table

            #region VAT Label
            if (_data.Members.Any(x => !x.Chargeable))
            {
                var _notChargeable = section.AddParagraph().AppendText($"\n*** Not Chargeable");
                ApplyStyleText(ref _notChargeable, Color.Red);
            }
            #endregion VAT Label

            var _filePath = $"{TimesheetSaveLocation()} {_data.TeamName} Report {_data.PeriodSearched.ToString("MMMM yyyy")}";

            doc.SaveToFile(_filePath + ".docx", FileFormat.Docx2013);

            return Json(_data, JsonRequestBehavior.AllowGet);

        }

        private void ApplyStyleText(ref TextRange text, Color? textColor, string fontName = "Verdana", int fontSize = 10, bool bold = true)
        {
            text.CharacterFormat.FontName = fontName;
            text.CharacterFormat.FontSize = fontSize;
            text.CharacterFormat.Bold = bold;
            text.CharacterFormat.TextColor = textColor ?? Color.DarkBlue;
        }

        private void CreateSummaryTable(ref Document doc, ref Table _table, string periodSearched, string excVAT, string incVAT, bool coreTeam)
        {

            IList<string> headerSummaryTable = new List<string>();
            IList<string> bodySummaryTable = new List<string>();

            //HEADER
            headerSummaryTable.Add($"{(coreTeam == true ? "Core" : "Drawdown")} Team Billing\n{periodSearched}");
            headerSummaryTable.Add("Total");

            //BODY
            bodySummaryTable.Add($"{(coreTeam == true ? "Core" : "Drawdown")} Team");
            if (coreTeam) bodySummaryTable.Add("Core Team Substitutes");
            bodySummaryTable.Add("Expenses");
            if (coreTeam) bodySummaryTable.Add("Less Warranty");
            bodySummaryTable.Add("Total (Excl.VAT)");
            bodySummaryTable.Add("Total (Incl.VAT)");

            _table.ResetCells((bodySummaryTable.Count + 1), 2);

            _table.TableFormat.HorizontalAlignment = RowAlignment.Center;
            _table.TableFormat.IsAutoResized = true;
            _table.TableFormat.IsBreakAcrossPages = false;

            TableRow headerRow = _table.Rows[0];
            headerRow.IsHeader = true;
            headerRow.Height = 16;
            headerRow.RowFormat.BackColor = Color.DarkBlue;

            #region Header

            ParagraphStyle headerStyle = new ParagraphStyle(doc);
            headerStyle.Name = "Header " + _table.Title;
            headerStyle.CharacterFormat.FontName = "Verdana";
            headerStyle.CharacterFormat.FontSize = 9;
            headerStyle.CharacterFormat.Bold = false;
            headerStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
            headerStyle.ParagraphFormat.TextAlignment = TextAlignment.Center;
            headerStyle.ParagraphFormat.WordWrap = false;
            doc.Styles.Add(headerStyle);

            foreach (var item in headerSummaryTable.Select((value, i) => new { i, value }))
            {
                _table[0, item.i].AddParagraph().AppendText(item.value); ;
            }

            //APPLY STYLE TO THE HEADER
            for (int i = 0; i < _table.Rows[0].Cells.Count; i++)
            {
                TableCell cell = _table.Rows[0].Cells[i];
                cell.CellFormat.VerticalAlignment = VerticalAlignment.Middle;
                cell.CellFormat.TextWrap = false;

                foreach (Paragraph para in cell.Paragraphs)
                {
                    //apply style
                    para.ApplyStyle(headerStyle.Name);
                }
            }
            #endregion Header

            #region Table Body

            ParagraphStyle bodyStyle = new ParagraphStyle(doc);
            bodyStyle.Name = "Body " + _table.Title;
            bodyStyle.CharacterFormat.FontName = "Verdana";
            bodyStyle.CharacterFormat.FontSize = 9;
            bodyStyle.CharacterFormat.Bold = false;
            bodyStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
            bodyStyle.ParagraphFormat.TextAlignment = TextAlignment.Center;
            bodyStyle.ParagraphFormat.WordWrap = false;
            doc.Styles.Add(bodyStyle);

            if (coreTeam)
            {
                //FIRST BODY ROW
                _table[1, 0].AddParagraph().AppendText(bodySummaryTable[0]);
                _table[1, 1].AddParagraph().AppendText($"€{decimal.Parse(excVAT):n}");

                _table[2, 0].AddParagraph().AppendText(bodySummaryTable[1]);
                _table[2, 1].AddParagraph().AppendText($"€0");

                _table[3, 0].AddParagraph().AppendText(bodySummaryTable[2]);
                _table[3, 1].AddParagraph().AppendText($"€0");

                _table[4, 0].AddParagraph().AppendText(bodySummaryTable[3]);
                _table[4, 1].AddParagraph().AppendText($"€0");

                TextRange _text;
                _text = _table[5, 0].AddParagraph().AppendText(bodySummaryTable[4]);
                _text.CharacterFormat.Bold = true;
                _text = _table[5, 1].AddParagraph().AppendText($"€{decimal.Parse(excVAT):n}");
                _text.CharacterFormat.Bold = true;

                _text = _table[6, 0].AddParagraph().AppendText(bodySummaryTable[5]);
                _text.CharacterFormat.Bold = true;
                _text = _table[6, 1].AddParagraph().AppendText($"€{decimal.Parse(incVAT):n}");
                _text.CharacterFormat.Bold = true;
            }
            else
            {
                //FIRST BODY ROW
                _table[1, 0].AddParagraph().AppendText(bodySummaryTable[0]);
                _table[1, 1].AddParagraph().AppendText($"€{decimal.Parse(excVAT):n}");

                _table[2, 0].AddParagraph().AppendText(bodySummaryTable[1]);
                _table[2, 1].AddParagraph().AppendText($"€0");

                TextRange _text;
                _text = _table[3, 0].AddParagraph().AppendText(bodySummaryTable[2]);
                _text.CharacterFormat.Bold = true;
                _text = _table[3, 1].AddParagraph().AppendText($"€{decimal.Parse(excVAT):n}");
                _text.CharacterFormat.Bold = true;

                _text = _table[4, 0].AddParagraph().AppendText(bodySummaryTable[3]);
                _text.CharacterFormat.Bold = true;
                _text = _table[4, 1].AddParagraph().AppendText($"€{decimal.Parse(incVAT):n}");
                _text.CharacterFormat.Bold = true;
            }
            

            //APPLY STYLE TO THE BODY TABLE
            for (int i = 1; i < _table.Rows.Count; i++) //starting from row 1 (avoiding Header)
            {
                for (int j = 0; j < _table.Rows[i].Cells.Count; j++)
                {
                    TableCell cell = _table.Rows[i].Cells[j];
                    cell.CellFormat.VerticalAlignment = VerticalAlignment.Middle;
                    cell.CellFormat.TextWrap = false;

                    foreach (Paragraph para in cell.Paragraphs)
                    {
                        //apply style
                        para.ApplyStyle(bodyStyle.Name);
                    }
                }
            }

            _table.TableFormat.Paddings.All = 3f;
            _table.AutoFit(AutoFitBehaviorType.AutoFitToContents);

            #endregion Table Body
        }

        private void CreateMemberTable(ref Document doc, ref Table _table, IList<string> Header, IList<ConsolidatedRateMonthly> Body)
        {

            _table.ResetCells((Body.Count + 2), Header.Count);

            _table.TableFormat.HorizontalAlignment = RowAlignment.Center;
            _table.TableFormat.IsAutoResized = true;
            _table.TableFormat.IsBreakAcrossPages = false;
            
            TableRow headerRow = _table.Rows[0];
            headerRow.IsHeader = true;
            headerRow.Height = 16;
            headerRow.RowFormat.BackColor = Color.DarkBlue;

            #region Header

            ParagraphStyle headerStyle = new ParagraphStyle(doc);
            headerStyle.Name = "Header " + _table.Title;
            headerStyle.CharacterFormat.FontName = "Verdana";
            headerStyle.CharacterFormat.FontSize = 9;
            headerStyle.CharacterFormat.Bold = false;
            headerStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
            headerStyle.ParagraphFormat.TextAlignment = TextAlignment.Center;
            headerStyle.ParagraphFormat.WordWrap = false;
            doc.Styles.Add(headerStyle);

            foreach (var item in Header.Select((value, i) => new { i, value }))
            {
                _table[0, item.i].AddParagraph().AppendText(item.value); ;
            }

            //APPLY STYLE TO THE BODY TABLE
            for (int i = 0; i < _table.Rows[0].Cells.Count; i++)
            {
                TableCell cell = _table.Rows[0].Cells[i];
                cell.CellFormat.VerticalAlignment = VerticalAlignment.Middle;
                cell.CellFormat.TextWrap = false;

                foreach (Paragraph para in cell.Paragraphs)
                {
                    //apply style
                    para.ApplyStyle(headerStyle.Name);
                }
            }

            #endregion Header

            #region Table Body

            ParagraphStyle bodyStyle = new ParagraphStyle(doc);
            bodyStyle.Name = "Body " + _table.Title;
            bodyStyle.CharacterFormat.FontName = "Verdana";
            bodyStyle.CharacterFormat.FontSize = 9;
            bodyStyle.CharacterFormat.Bold = false;
            bodyStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
            bodyStyle.ParagraphFormat.TextAlignment = TextAlignment.Center;
            bodyStyle.ParagraphFormat.WordWrap = false;
            doc.Styles.Add(bodyStyle);

            var rowIndex = 0;
            IList<TextRange> notChargeableMembers = new List<TextRange>();
            foreach (var item in Body.Select((value, i) => new { i, value }))
            {
                rowIndex = item.i + 1;
                
                _table[rowIndex, 1].AddParagraph().AppendText(item.value.DaysWorked.ToString());
                _table[rowIndex, 2].AddParagraph().AppendText(item.value.ChargeableHours.ToString());
                _table[rowIndex, 3].AddParagraph().AppendText($"€{item.value.RateExcVat:n}");
                _table[rowIndex, 4].AddParagraph().AppendText(item.value.Role);
                if (item.value.Chargeable)
                {
                    _table[rowIndex, 0].AddParagraph().AppendText(item.value.MemberName);
                    _table[rowIndex, 5].AddParagraph().AppendText($"€{item.value.DayRateExcVat:n}");
                }
                else
                {
                    notChargeableMembers.Add(_table[rowIndex, 0].AddParagraph().AppendText($"*** {item.value.MemberName}"));
                    _table[rowIndex, 5].AddParagraph().AppendText($"€0");
                }
            }

            //APPLY STYLE TO THE HEADER
            for (int i = 1; i < _table.Rows.Count; i++) //starting from row 1 (avoiding Header)
            {
                for (int j = 0; j < _table.Rows[i].Cells.Count; j++)
                {
                    TableCell cell = _table.Rows[i].Cells[j];
                    cell.CellFormat.VerticalAlignment = VerticalAlignment.Middle;
                    cell.CellFormat.TextWrap = false;

                    foreach (Paragraph para in cell.Paragraphs)
                    {
                        //apply style
                        para.ApplyStyle(bodyStyle.Name);
                    }
                }
            }

            //highlight color (red) for non-chargeable members
            foreach (var item in notChargeableMembers)
            {
                item.CharacterFormat.TextColor = Color.Red;
            }

            var lastRow = _table.LastRow.Cells[0].AddParagraph();
            var lastRowText = lastRow.AppendText("Total (excl. VAT)");
            lastRow.ApplyStyle(headerStyle);
            lastRowText.CharacterFormat.Bold = true;

            var totalAmountExclVAT = _table.LastRow.Cells[_table.LastRow.Cells.Count - 1].AddParagraph();
            var totalAmountExclVATText = totalAmountExclVAT.AppendText($"€{Body.Sum(x=>x.DayRateExcVat):n}");
            totalAmountExclVAT.ApplyStyle(headerStyle);
            totalAmountExclVATText.CharacterFormat.Bold = true;

            _table.TableFormat.Paddings.All = 3f;
            _table.AutoFit(AutoFitBehaviorType.AutoFitToContents);

            #endregion Table Body

        }

        #endregion Create Word Doc

    }

}