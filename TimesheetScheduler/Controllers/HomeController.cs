using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Web.Mvc;
using System.Globalization;
using System.Net;
using System.Configuration;
//using System.Runtime.InteropServices;
//using System.Linq;
//using System.Web;

//using TimesheetScheduler.Models;
using Microsoft.Office.Interop.Excel;

using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using TimesheetScheduler.Models;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Application = Microsoft.Office.Interop.Excel.Application;
using System.Linq;

namespace TimesheetScheduler.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //CreateTaskOnTFS();
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

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


        #region TFS

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
            //return domainUserName.Replace("/","");
        }

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

        public JsonResult ConnectTFS(bool bypassTFS, string userName, int _month, int _year)
        {
            //if (!bypassTFS)
            //{
            IList<IList<WorkItemSerialized>> joinWorkItemsList = new List<IList<WorkItemSerialized>>();
            userName = userName.Replace("'", "''");
            joinWorkItemsList.Add(ReturnTFSEvents_ListWorkItems(bypassTFS, userName, _month, _year));
            joinWorkItemsList.Add(ReturnTFSEvents_ListWorkItemsWithoutStartDate(bypassTFS, userName, _month, _year));

            return Json(joinWorkItemsList, JsonRequestBehavior.AllowGet);
            //}

            //return Json(new EmptyResult(), JsonRequestBehavior.AllowGet);
        }

        public IList<WorkItemSerialized> ReturnTFSEvents_ListWorkItems(bool _bypass, string userName, int _month, int _year)
        {

            IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();

            if (!_bypass)
            {
                Uri tfsUri = new Uri(GetUrlTfs());
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                var projectName = GetProjectNameTFS();
                var _iterationPath = GetIterationPathTFS();
                var _userLogged = GetUserName();

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
                    //" AND [Assigned To] =" + "'Ian O''Brien'" +
                    " ORDER BY [System.Id], [System.WorkItemType]");

                foreach (WorkItem wi in WIC)
                {
                    if (wi["Start Date"] != null)
                    {
                        DateTime _startDate = (DateTime)wi["Start Date"];
                        if (_startDate.Month == (_month == 0 ? DateTime.Now.Month : _month)
                            && _startDate.Year == (_year == 0 ? DateTime.Now.Year : _year))
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
                                Description = wi["Description"].ToString(),
                                CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : (double?)null,
                                WorkItemsLinked = _workItemsLinked
                            });
                        }
                    }
                }
            }
            else
            {
                //---------------------
                for (int i = 0; i < DateTime.DaysInMonth(_year, _month); i++)
                {
                    //few days without info to simulate "out of the office"
                    if (i == 7 || i == 12 || i == 17 || i == 22 || i == 28) continue;
                    listWorkItems.Add(new WorkItemSerialized()
                    {
                        Id = "35541" + i,
                        Title = "Title-" + i,
                        StartDate = new DateTime(_year, _month, i + 1),
                        Description = "Description-" + i,
                        CompletedHours = i + 1,
                        WorkItemsLinked = "#321321 #654654"
                    });
                }
            }

            return listWorkItems;
        }

        public IList<WorkItemSerialized> ReturnTFSEvents_ListWorkItemsWithoutStartDate(bool _bypass, string userName, int _month, int _year)
        {
            IList<WorkItemSerialized> listWorkItemsWithoutStartDate = new List<WorkItemSerialized>();
            if (!_bypass)
            {
                Uri tfsUri = new Uri(GetUrlTfs());
                TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
                WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

                var projectName = GetProjectNameTFS();
                var _iterationPath = GetIterationPathTFS();
                var _userLogged = GetUserName();

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



                foreach (WorkItem wi in WIC)
                {
                    if (wi["Start Date"] == null)
                    {
                        listWorkItemsWithoutStartDate.Add(new WorkItemSerialized()
                        {
                            Id = wi["Id"].ToString(),
                            Title = wi["Title"].ToString(),
                        });

                    }
                }
            }
            else
            {
                for (int i = 0; i < DateTime.DaysInMonth(_year, _month); i++)
                {
                    listWorkItemsWithoutStartDate.Add(new WorkItemSerialized()
                    {
                        Id = "35541" + i,
                        Title = "Title-" + i
                    });
                }
            }
            return listWorkItemsWithoutStartDate;

        }

        public IList<WorkItemRecord> Convert_TFS_Events_To_Excel_Format(IList<WorkItemSerialized> _events, int _month, int _year)
        {
            IList<WorkItemRecord> timesheetRecords = new List<WorkItemRecord>();
            var _days = DateTime.DaysInMonth(_year, _month);
            DateTime day;
            var _index = 0;

            //loop for all days in the month but doesn't include weekends
            //allow more then 1 task per day
            for (int i = 0; i < _days; i++) //_events.Count; i++) //
            {
                day = new DateTime(_year, _month, i + 1);
                if (IsWeekend(day))
                {
                    timesheetRecords.Add(new WorkItemRecord
                    {
                        Id = (i + 1),
                        Date = day,
                        WorkItemNumber = 0,
                        Description = "",
                        ChargeableHours = 0,
                        NonChargeableHours = 0,
                        Comments = "",
                        IsWeekend = IsWeekend(day)
                    });
                }
                else
                {
                    var _itemEvent = _events.Where(x => x.StartDate.Value.ToShortDateString() == day.ToShortDateString());
                    if (_itemEvent.Count() > 0)
                    {
                        foreach (var item in _itemEvent)
                        {
                            timesheetRecords.Add(new WorkItemRecord
                            {
                                Id = (i + 1),
                                Date = item.StartDate.Value,
                                WorkItemNumber = int.Parse(item.Id),
                                Description = item.Title, //item.Description,
                                ChargeableHours = item.CompletedHours == null ? 0 : (float)item.CompletedHours,
                                NonChargeableHours = (float)item.CompletedHours,
                                Comments = item.WorkItemsLinked,
                                IsWeekend = IsWeekend(item.StartDate.Value)
                            });
                        }
                    }
                    else //does not exist this date in the events
                    {
                        timesheetRecords.Add(new WorkItemRecord
                        {
                            Id = (i + 1),
                            Date = day,
                            WorkItemNumber = 0,
                            Description = "Out of the office",
                            ChargeableHours = 0,
                            NonChargeableHours = 0,
                            Comments = "------",
                            IsWeekend = IsWeekend(day)
                        });
                    }
                }
            }
            return timesheetRecords;
        }

        public string CreateTaskOnTFS(string startDate, string description, float chargeableHours)//newTimesheetTask
        {
            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();
            var _userLogged = GetUserName();

            Uri tfsUri = new Uri(GetUrlTfs());
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            Project teamProject = WIS.Projects.GetById(3192); //3192 = BOM_MOD24 [35]
            WorkItemType workItemType = teamProject.WorkItemTypes["Task"];

            WorkItem newWI = new WorkItem(workItemType);
            newWI.Title = description;
            newWI.Fields["System.AssignedTo"].Value = _userLogged;
            newWI.Fields["System.TeamProject"].Value = projectName;
            newWI.Fields["Iteration Path"].Value = _iterationPath;
            newWI.Fields["Completed Work"].Value = chargeableHours;
            newWI.Fields["Start Date"].Value = Convert.ToDateTime(startDate);
            //var _valid = newWI.Validate();
            newWI.Save();

            return newWI.Fields["ID"].Value.ToString();
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
            //return "Timesheet" + separator + NameSplittedByUnderscore(FormatDomainUserName(GetDomainUserName()), separator.ToString()) + separator + _monthFormatted + separator + _year;
            return "Timesheet" + separator + NameSplittedByUnderscore(userName, separator.ToString()) + separator + _monthFormatted + separator + _year;
        }

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
            //UserName = FormatDomainUserName(GetDomainUserName()); //GetUserName();
            UserName = userName; //GetUserName();
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
                    //borderLineStyle = XlLineStyle.xlContinuous,
                    //borderColor = Color.Gray
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
                CellValue = "=SUM(E6:E50)", //TODO: apply formula
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
                CellValue = "=SUM(F6:F50)", //TODO: apply formula
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
                CellValue = "Description",
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
                CellValue = "Non-Chargeable Hours",
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
                CellValue = "Comments",
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

        private void CreateTable(Worksheet worksheet, bool _bypassTFS, string userName, int _month, int _year)
        {
            var _table = TimesheetRecords(_bypassTFS, userName, _month, _year);
            int firstRow = 6;
            int lastRow = _table.Count + firstRow;
            int j;

            for (int i = firstRow; i < lastRow; i++) //ROW
            {
                j = i - firstRow;
                worksheet.Cells[i, 1] = _table[j].Id;
                worksheet.Cells[i, 2] = _table[j].Date;
                worksheet.Cells[i, 3] = _table[j].WorkItemNumber;
                worksheet.Cells[i, 4] = _table[j].Description;
                worksheet.Cells[i, 5] = _table[j].ChargeableHours;
                worksheet.Cells[i, 6] = _table[j].NonChargeableHours;
                worksheet.Cells[i, 7] = _table[j].Comments;

                worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].VerticalAlignment = XlHAlign.xlHAlignCenter;

                worksheet.Cells[i, 3].EntireColumn.NumberFormat = "0";

                if (_table[j].IsWeekend)
                {
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.DarkGray; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = true;
                }
                else
                {
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Interior.Color = Color.White; //TODO
                    worksheet.Range[worksheet.Cells[i, 1], worksheet.Cells[i, 7]].Locked = false;
                }
            }
        }

        public void SetValAndFormatCell(Worksheet worksheet, CellObject cellObj)
        {
            SetCellValue(worksheet, cellObj);
            FormatCell(worksheet, cellObj);
        }

        public string SaveExcelFile(bool _bypassTFS, string userName, int _month, int _year)
        {
            Application excel;
            Workbook worKbooK;
            Worksheet worksheet;
            Range celLrangE;

            try
            {
                excel = new Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);

                InitExcelVariables(userName, _month, _year);

                worksheet = (Worksheet)worKbooK.ActiveSheet;
                worksheet.Name = "Timesheet"; //TODO: create a better file name using month and year

                CreateHeader(worksheet);
                CreateTable(worksheet, _bypassTFS, userName, _month, _year);
                resizeColumns(worksheet);

                celLrangE = worksheet.Range[worksheet.Cells[5, 1], worksheet.Cells[36, 7]]; //TODO
                Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
                border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                border.Weight = 2d;

                protectSheet(worksheet);
                var saveParams = TimesheetSaveLocationAndFileName(userName, _month, _year);
                worKbooK.SaveAs(saveParams);

                worKbooK.Close();
                excel.Workbooks.Close();
                excel.Quit();
                Marshal.ReleaseComObject(worksheet);//avoid opening excel windows with previously generated files by the program when system restarts
                Marshal.ReleaseComObject(worKbooK);
                return "OK from backend";
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

        //-0------------------------------------------------------------------------------------------------------

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

        //-0------------------------------------------------------------------------------------------------------

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

        public IList<WorkItemRecord> TimesheetRecords(bool _bypassTFS, string userName, int _month, int _year)
        {
            return Convert_TFS_Events_To_Excel_Format(ReturnTFSEvents_ListWorkItems(_bypassTFS, userName, _month, _year), _month, _year);
        }

        #endregion EXCEL

        private bool IsWeekend(DateTime day)
        {
            return (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday);
        }
    }
}

public class WorkItemSerialized
{

    public string Id { get; set; }
    public string Title { get; set; }
    public DateTime? StartDate { get; set; }
    public string Description { get; set; }
    public double? CompletedHours { get; set; }
    public string WorkItemsLinked { get; set; }

}

public class WorkItemRecord
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int WorkItemNumber { get; set; }

    public string Description { get; set; }

    public float ChargeableHours { get; set; }

    public float NonChargeableHours { get; set; }

    public string Comments { get; set; }

    public bool IsWeekend { get; set; }
}

public class ParamsFormatCell
{
    public int fontSize { get; set; } = 11;
    public string fontFamily { get; set; }
    public bool fontBold { get; set; } = false;
    public bool fontItalic { get; set; } = false;
    public Color fontColor { get; set; } = Color.Black;
    public Color cellBackgroundColor { get; set; } = Color.Black;
    public XlHAlign aligment { get; set; } = XlHAlign.xlHAlignCenter;
    public bool lockCell { get; set; } = false;

    //public XlLineStyle borderLineStyle { get; set; }
    //public Color borderColor { get; set; }
}

public class CellObject
{
    public string CellPosition { get; set; }
    public string CellValue { get; set; }
    public ParamsFormatCell FormatParams { get; set; }
}