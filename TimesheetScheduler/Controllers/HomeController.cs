using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimesheetScheduler.Models;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using System.Globalization;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Net;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System.Configuration;

namespace TimesheetScheduler.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ExcelExport excelFileExport = new ExcelExport();
            excelFileExport.ExcelFileExport();
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

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
            return UserPrincipal.Current.DisplayName;
        }

        public string GetDomainUserName()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }
        //@
        public JsonResult ConnectTFS()
        {
            Uri tfsUri = new Uri(GetUrlTfs());
            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            var projectName = GetProjectNameTFS();
            var _iterationPath = GetIterationPathTFS();
            var _userLogged = UserPrincipal.Current.DisplayName;

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
                " AND [Assigned To] = '" + _userLogged + "'" +
                " ORDER BY [System.Id], [System.WorkItemType]");

            IList<IList<WorkItemSerialized>> joinWorkItemsList = new List<IList<WorkItemSerialized>>();
            IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();
            IList<WorkItemSerialized> listWorkItemsWithoutStartDate = new List<WorkItemSerialized>();

            foreach (WorkItem wi in WIC)
            {
                if (wi["Start Date"] != null)
                {
                    DateTime _startDate = (DateTime)wi["Start Date"];
                    if (_startDate.Month == DateTime.Now.Month)
                    {
                        listWorkItems.Add(new WorkItemSerialized()
                        {
                            Id = wi["Id"].ToString(),
                            Title = wi["Title"].ToString(),
                            StartDate = wi["Start Date"] != null ? (DateTime)wi["Start Date"] : (DateTime?)null,
                            Description = wi["Description"].ToString(),
                            CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : (double?)null

                            /*
                            Id: (i + 1),
                            tooltipDay: _isWeekend ? "WEEKEND" : "",
                            classRow: _isWeekend ? "weekendRow" : "weekdayRow",
                            disableFlag: _isWeekend ? "disabled" : "",
                            dayShortFormat: formatDate(new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i)),
                            day: new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i).toDateString(),
                            workItem: "34500" + (i + 1),
                            description: "description... " + (i + 1),
                            //chargeableHours: "6.4",
                            chargeableHours: "7.5",
                            nonchargeableHours: "2.0",
                            comments: "comments... " + (i + 1)
                             */
                        });
                    }
                }
                else
                {
                    listWorkItemsWithoutStartDate.Add(new WorkItemSerialized()
                    {
                        Id = wi["Id"].ToString(),
                        Title = wi["Title"].ToString(),
                    });

                }
            }

            joinWorkItemsList.Add(listWorkItems);
            joinWorkItemsList.Add(listWorkItemsWithoutStartDate);

            return Json(joinWorkItemsList, JsonRequestBehavior.AllowGet);
        }

        #region UTIL

        public string NameSplittedByUnderscore(string name, string separator)
        {
            return name.Replace(" ", separator);
        }

        public string TimesheetFileName(char separator)
        {
            return "Timesheet" + separator + NameSplittedByUnderscore(UserName, separator.ToString()) + separator + MonthTimesheet + separator + YearTimesheet;
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


        //---------------------------------------- END HEADER TABLE---------------------------------------------

        #endregion Const Cell Range and Labels

        public void InitExcelVariables()
        {
            UserName = "GIOVANNI RUSSO BOSCOLI";
            MonthTimesheet = DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            YearTimesheet = DateTime.Now.Year.ToString();

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
                CellValue = DateTime.Now.ToString("MMM/yyyy", CultureInfo.CreateSpecificCulture("en")), //TODO DYNAMIC
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

        private void CreateTable(Worksheet worksheet)
        {
            var _table = TimesheetRecords();
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

        public void ExcelExport()
        {
            Application excel;
            Microsoft.Office.Interop.Excel.Workbook worKbooK;
            Microsoft.Office.Interop.Excel.Worksheet worksheet;
            Microsoft.Office.Interop.Excel.Range celLrangE;

            try
            {
                excel = new Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);

                InitExcelVariables();

                worksheet = (Worksheet)worKbooK.ActiveSheet;
                worksheet.Name = "Timesheet"; //TODO: create a better file name using month and year

                CreateHeader(worksheet);
                CreateTable(worksheet);
                resizeColumns(worksheet);
                excelAddButtonWithVBA(worKbooK);
                //border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                //border.Weight = 2d;
                //worksheet.Cells.Font.Size = 27;

                celLrangE = worksheet.Range[worksheet.Cells[5, 1], worksheet.Cells[36, 7]]; //TODO
                Microsoft.Office.Interop.Excel.Borders border = celLrangE.Borders;
                border.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                border.Weight = 2d;
                //WorkbookSheetChange(worKbooK);

                ////welfare.irlgov.ie/shares/FRDIRWIN7_FI/giovanniboscoli/Desktop/
                protectSheet(worksheet);
                worKbooK.SaveAs(TimesheetFileName('_'));

                worKbooK.Close();
                excel.Quit();

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
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

            //MessageBox.Show("The value of " + sheet.Name + ":" +
            //  changedRange + " was changed.");
        }

        private static void excelAddButtonWithVBA(Workbook xlBook)
        {
            Application xlApp = new Application();
            // Excel.Workbook xlBook = xlApp.Workbooks.Open(@"PATH_TO_EXCEL_FILE");
            Worksheet wrkSheet = xlBook.Worksheets[2];
            Range range;

            try
            {
                //set range for insert cell
                range = wrkSheet.get_Range("A1:A1");

                //insert the dropdown into the cell
                Buttons xlButtons = wrkSheet.Buttons();
                Button xlButton = xlButtons.Add((double)range.Left, (double)range.Top, (double)range.Width, (double)range.Height);

                //set the name of the new button
                xlButton.Name = "btnDoSomething";
                xlButton.Text = "Click me!";
                xlButton.OnAction = "btnDoSomething_Click";
                //xlButton.Formula = "MsgBox teste";

                //buttonMacro(xlButton.Name, xlApp, xlBook, wrkSheet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //xlApp.Visible = true;
        }

        //private static void buttonMacro(string buttonName, Application xlApp, Workbook wrkBook, Worksheet wrkSheet)
        //{
        //    StringBuilder sb;
        //    VBIDE.VBComponent xlModule;
        //    VBIDE.VBProject prj;

        //    prj = wrkBook.VBProject;
        //    sb = new StringBuilder();

        //    // build string with module code
        //    sb.Append("Sub " + buttonName + "_Click()" + "\n");
        //    sb.Append("\t" + " msgbox \"" + buttonName + "\"\n"); // add your custom vba code here
        //    sb.Append(" End Sub");

        //    // set an object for the new module to create
        //    xlModule = wrkBook.VBProject.VBComponents.Add(VBIDE.vbext_ComponentType.vbext_ct_StdModule);

        //    // add the macro to the spreadsheet
        //    xlModule.CodeModule.AddFromString(sb.ToString());
        //}

        //-0------------------------------------------------------------------------------------------------------

        private void protectSheet(Worksheet worksheet)
        {
            //worksheet.Range[worksheet.Cells[1, 7], worksheet.Cells[4, 7]].Style.Locked = true; //lock header
            //worksheet.Columns[1].Style.Locked = true; //lock Id Column
            //worksheet.Columns[2].Style.Locked = true; //lock Date Column
            //worksheet.Protect("bom", UserInterfaceOnly: true);
            //worksheet.Range[worksheet.Cells[1, 7], worksheet.Cells[4, 7]].Style.Locked = false; //lock header
            //worksheet.Range.Style.Locked = false;
            //worksheet.Range["A1:B3"].Style.Locked = true;
            //worksheet.Protect("123", SheetProtectionType.All);

            //worksheet.Columns[2].Style.Locked = false; //lock Date Column

            //worksheet.Columns.Locked = false;
            //((Microsoft.Office.Interop.Excel.Range)worksheet.get_Range((object)worksheet.Cells[1,7], (object)worksheet.Cells[4, 7])).EntireColumn.Locked = true;
            //worksheet.EnableSelection = Microsoft.Office.Interop.Excel.XlEnableSelection.xlUnlockedCells;
            //wks.Protect(mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv, mv);
            //worksheet.Cells.Style.Locked = false;
            //worksheet.Columns[1].Style.Locked = true;
            //worksheet.Range[worksheet.Cells[1, 7], worksheet.Cells[4, 7]].Style.Locked = false;
            //worksheet.Range[worksheet.Cells[6, 1], worksheet.Cells[36, 7]].Style.Locked = true;
            //worksheet.Protect("bom", AllowFormattingColumns: true, AllowFormattingRows: true, UserInterfaceOnly: true);
            //worksheet.Protect("bom", Type.Missing,true,Type.Missing, Type.Missing, Type.Missing, Type.Missing,Type.Missing, 
            // Type.Missing, Type.Missing, Type.Missing,Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

            var missing = Type.Missing;
            //worksheet.Cells.Locked = false;
            worksheet.Columns[1].Locked = true;
            worksheet.Columns[2].Locked = true;
            //worksheet.Range[].Locked = true;
            //worksheet.Range[worksheet.Columns[2]].Locked = true;
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
            //worksheet.Cells[11, 7].Style.WrapText = true; //does not work
            worksheet.get_Range("G11").WrapText = true;
        }

        public IList<WorkItemRecord> TimesheetRecords()
        {
            IList<WorkItemRecord> timesheetRecords = new List<WorkItemRecord>();
            DateTime currentPeriod = DateTime.Now;
            var _days = DateTime.DaysInMonth(currentPeriod.Year, currentPeriod.Month);
            DateTime day;
            for (int i = 1; i <= _days; i++)
            {
                day = new DateTime(currentPeriod.Year, currentPeriod.Month, i);

                timesheetRecords.Add(new WorkItemRecord
                {
                    Id = i,
                    Date = day,
                    WorkItemNumber = (345450 + i),
                    Description = i == 3 ? "Timesheet - DeployP31/Nof7.2/Rebase Investigation branch/ case load summary bug #346796 #318197 #346798 #345474" : "Description-" + i,
                    ChargeableHours = 7.5F,
                    NonChargeableHours = 0.0F,
                    Comments = i == 6 ? "Timesheet - DeployP31/Nof7.2/Rebase Investigation branch/ case load summary bug #346796 #318197 #346798 #345474" : "Comments-" + i,
                    IsWeekend = IsWeekend(day)
                });
            }

            return timesheetRecords;
        }

        private bool IsWeekend(DateTime day)
        {
            return (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday);
        }

        public void ConnectToTFS()
        {
            //GiovanniBoscoli@welfare.irlgov.ie
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/f0bcdca8-60fc-4be3-ab64-2575589fc765/programatically-connecting-to-tfs?forum=tfsgeneral
            //NetworkCredential networkCredentials = new NetworkCredential(@"Domain\Account", @"Password");
            //Microsoft.VisualStudio.Services.Common.WindowsCredential windowsCredentials = new Microsoft.VisualStudio.Services.Common.WindowsCredential(networkCredentials);
            //VssCredentials basicCredentials = new VssCredentials(windowsCredentials);
            //TfsTeamProjectCollection tfsColl = new TfsTeamProjectCollection(
            //    new Uri("http://XXX:8080/tfs/DefaultCollection"),
            //    basicCredentials);

            //tfsColl.Authenticate(); // make sure it is authenticate

            //Uri collectionUri = new Uri("https://MyName.visualstudio.com/DefaultCollection");

            //Uri collectionUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS"); //working
            //NetworkCredential credential = new NetworkCredential("GiovanniBoscoli@welfare.irlgov.ie", "?bCh+*p#d8MQ07");

            //Uri collectionUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/BOM_MOD24/BOMi%20UI%20Design/_queries");
            //Uri collectionUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/BOM_MOD24/BOMi%20UI%20Design/_queries?id=4cc9e796-664b-4e1e-b3df-7ebf43525e89&_a=query");
            //Uri collectionUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/INTEG_MIRROR");
            //NetworkCredential credential = new NetworkCredential(@"WELFARE\GiovanniBoscoli", "?bCh+*p#d8MQ07");
            //NetworkCredential credential = new NetworkCredential("GiovanniBoscoli@welfare.irlgov.ie", "?bCh+*p#d8MQ07");

            //TfsTeamProjectCollection teamProjectCollection = new TfsTeamProjectCollection(collectionUri, credential);
            //teamProjectCollection.EnsureAuthenticated();


            using (TfsTeamProjectCollection tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/")))
            {
                tpc.EnsureAuthenticated();

                // i am getting workitemstore object here
                var wiStore = tpc.GetService<WorkItemStore>();

                // i am getting version control server object here as well
                var vcs = tpc.GetService<VersionControlServer>();

                var a = vcs.GetAllTeamProjects(true);
                var b = vcs.GetTeamProject("BOM_MOD24");
                var c = b.TeamProjectCollection;

                //using (TfsTeamProjectCollection tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/")))
                //{

                //}
                //List<int> ids = new List<int>();
                //ids.Add(349423);
                //var items = vcs.GetItems(ids.ToArray(), 1);

                var d = b.TeamProjectCollection.GetService<WorkItemStore>();

                //Workspace ws = vcs.GetWorkspace("$/BOM_MOD24", "BOM_MOD24");

                //var path = "$/BOM_MOD24/BOMi%20UI%20Design/_queries";
                //var path = "$/BOM_MOD24/BOMi%20UI%20Design/_queries?id=4cc9e796-664b-4e1e-b3df-7ebf43525e89&_a=query";
                var path = "$/BOM_MOD24";

                var myFolderAtChangeset17 = vcs.GetItems(path, new ChangesetVersionSpec(10), RecursionType.Full);

                var _query = "SELECT * FROM WorkItems"; // WHERE [System.WorkItemType] = 'Task' AND [Assigned to] = 'name' ORDER BY[System.WorkItemType], [System.Id]";


                WorkItemStore abc = (WorkItemStore)c.GetService(typeof(WorkItemStore));
                //var WIC = abc.Query(_query);

                //WorkItemStore _workItemStore = new WorkItemStore("BOM_MOD24");
                //WorkItemStore _workItemStore2 = new WorkItemStore(vcs.TeamProjectCollection);
                // but here i get a null object
                //var bs = tpc.GetService<IBuildServer>();

                ////this is what i want to do with buildserver object
                //var buildDefinition = bs.GetBuildDefinition("aaa", "bbb");
                //var buildRequest = buildDefinition.CreateBuildRequest();
                //bs.QueueBuild(buildRequest);

                WorkItemStore workItemStore = c.GetService<WorkItemStore>();

                if (workItemStore != null)
                {
                    WorkItemCollection workItemCollection = workItemStore.Query("QUERY HERE");

                    foreach (var item in workItemCollection)
                    {
                        //Do something here.
                    }
                }
            }
        }

        public void ConnectTFS2()
        {
            NetworkCredential credential = new NetworkCredential("GiovanniBoscoli@welfare.irlgov.ie", "?bCh+*p#d8MQ07");
            BasicAuthCredential basicCred = new BasicAuthCredential(credential);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
            tfsCred.AllowInteractive = false;

            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(
                //new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/INTEG_MIRROR"),
                new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/"),
                tfsCred);

            tpc.EnsureAuthenticated();

            Console.WriteLine(tpc.InstanceId);
        }

        public void ConnectTFS3()
        {
            //https://stackoverflow.com/questions/2455654/what-additional-configuration-is-necessary-to-reference-a-net-2-0-mixed-mode
            //SecureString secureString = new SecureString("?bCh+*p#d8MQ07",10);

            var pathBOM24 = "vstfs:///Classification/TeamProject/83822731-bd6e-4624-a6cf-45032d6c302a";

            NetworkCredential networkCredential = new NetworkCredential("GiovanniBoscoli@welfare.irlgov.ie", "?bCh+*p#d8MQ07");

            ICredentials credential = (ICredentials)networkCredential;

            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/"), credential);
            TfsTeamProjectCollection tfs2 = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/"));
            //TfsTeamProjectCollection tfs3 = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/BOM_MOD24"));            

            //WorkItemStore wi = new WorkItemStore();

            tfs.EnsureAuthenticated();
            tfs2.EnsureAuthenticated();
            //tfs3.EnsureAuthenticated();

            var vcs = tfs.GetService<VersionControlServer>();
            var a = vcs.GetAllTeamProjects(true);
            var b = vcs.GetTeamProject("BOM_MOD24");
            var c = b.TeamProjectCollection;
            var d = b.TeamProjectCollection.GetService<WorkItemStore>();

            var path = "$/BOM_MOD24";
            //WorkItemStore wi2 = new WorkItemStore(path);

            var myFolderAtChangeset17 = vcs.GetItems(path, new ChangesetVersionSpec(10), RecursionType.Full);

            //var bs = tfs2.GetService<IBuildServer>();

            WorkItemStore workitemstore = tfs.GetService<WorkItemStore>();
            WorkItemStore workitemstore2 = tfs2.GetService<WorkItemStore>();

            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/"));
            ICommonStructureService Iss = (ICommonStructureService)tfs2.GetService(typeof(ICommonStructureService));
            ProjectInfo[] ProjInfo = Iss.ListProjects();

            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));

            foreach (ProjectInfo pi in ProjInfo)
            {
                WorkItemCollection WIC = WIS.Query(
                " SELECT [System.Id], [System.WorkItemType]," +
                " [System.State], [System.AssignedTo], [System.Title] " +
                " FROM WorkItems " +
                " WHERE [System.TeamProject] = '" + pi.Name +
                "' ORDER BY [System.WorkItemType], [System.Id]");

                foreach (WorkItem wi in WIC)
                {
                    Console.WriteLine(wi.Id);
                    Console.WriteLine(wi.Title);
                }
            }

            if (workitemstore != null)
            {
                Console.WriteLine("… …");
            }
        }

        public void ConnectTFS4()
        {
            Uri tfsUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/");
            //string teamProjectName = "INTEG_MIRROR";
            string teamProjectName = "BOM_MOD24";
            TfsTeamProjectCollection projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            var css3 = projectCollection.GetService<ICommonStructureService3>();
            ProjectInfo projectInfo = css3.GetProjectFromName(teamProjectName);
            TfsTeamService teamService = projectCollection.GetService<TfsTeamService>();
            var allItems = teamService.QueryTeams(projectInfo.Uri);

            TeamFoundationTeam foundationTeam = projectCollection.GetService<TeamFoundationTeam>();
            var members = foundationTeam.GetMembers(projectCollection, MembershipQuery.Direct);
        }

        public JsonResult ConnectTFS5()
        //public IList<WorkItem> ConnectTFS5()
        {
            Uri tfsUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/");
            //string teamProjectName = "INTEG_MIRROR";
            //string teamProjectName = "BOM_MOD24";
            //var projectUri = "vstfs:///Classification/TeamProject/83822731-bd6e-4624-a6cf-45032d6c302a";

            //TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);

            //// Retrieve the default team.
            //TfsTeamService teamService = collection.GetService<TfsTeamService>();
            //TeamFoundationTeam defaultTeam = teamService.GetDefaultTeam(projectUri, null);

            //// Get security namespace for the project collection.
            //ISecurityService securityService = collection.GetService<ISecurityService>();
            //SecurityNamespace securityNamespace = securityService.GetSecurityNamespace(FrameworkSecurity.IdentitiesNamespaceId);

            //// Retrieve an ACL object for all the team members.
            //var allMembers = defaultTeam.GetMembers(collection, MembershipQuery.Expanded).Where(m => !m.IsContainer);

            // WorkItemStore store = (WorkItemStore)collection.GetService(typeof(WorkItemStore));

            //var teamProjectCollection = new TfsTeamProjectCollection(tfsUri);
            //var workItemStore = teamProjectCollection.GetService<WorkItemStore>();

            TfsTeamProjectCollection projCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            //ICommonStructureService Iss = (ICommonStructureService)projCollection.GetService(typeof(ICommonStructureService));
            //ProjectInfo[] ProjInfo = Iss.ListProjects();
            WorkItemStore WIS = (WorkItemStore)projCollection.GetService(typeof(WorkItemStore));
            //IList<string> strBuilder = new List<string>();

            var projectName = "BOM_MOD24";
            var _iterationPath = "BOM_MOD24\\Timesheets";
            var _assignedTo = "Giovanni Boscoli";

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
                " AND [Assigned To] = '" + _assignedTo + "'" +
                //" AND [Start Date] = '03/09/2019 00:00:00'" +
                //" AND MONTH([Start Date]) = '8'" +
                //" AND getdate([Start Date]) = @StartOfMonth " +
                //" AND DATEPART(month, [Start Date]) = " + "DATEPART(month, GETDATE())" + 
                //" AND ('2019-09-03') = " + " MONTH('2019-09-03') " + 
                " ORDER BY [System.WorkItemType], [System.Id]");

            IList<IList<WorkItemSerialized>> joinWorkItemsList = new List<IList<WorkItemSerialized>>();
            IList<WorkItemSerialized> listWorkItems = new List<WorkItemSerialized>();
            IList<WorkItemSerialized> listWorkItemsWithoutStartDate = new List<WorkItemSerialized>();

            foreach (WorkItem wi in WIC)
            {
                if (wi["Start Date"] != null)
                {
                    DateTime _startDate = (DateTime)wi["Start Date"];
                    if (_startDate.Month == DateTime.Now.Month)
                    {
                        listWorkItems.Add(new WorkItemSerialized()
                        {
                            Id = wi["Id"].ToString(),
                            Title = wi["Title"].ToString(),
                            StartDate = wi["Start Date"] != null ? (DateTime)wi["Start Date"] : (DateTime?)null,
                            Description = wi["Description"].ToString(),
                            CompletedHours = wi["Completed Work"] != null ? (double)wi["Completed Work"] : (double?)null
                        });
                    }
                }
                else
                {
                    listWorkItemsWithoutStartDate.Add(new WorkItemSerialized()
                    {
                        Id = wi["Id"].ToString(),
                        Title = wi["Title"].ToString(),
                    });

                }

                //if (wi.IterationPath.Equals(@"BOM_MOD24\Timesheets") && wi.CreatedBy.Equals("Giovanni Boscoli")) {
                //    var assignedTo = wi["Start Date"];
                //    strBuilder.Add(wi.Title);
                //    strBuilder.Add(wi.Description);
                //}
                //Console.WriteLine(wi.Title);

            }

            joinWorkItemsList.Add(listWorkItems);
            joinWorkItemsList.Add(listWorkItemsWithoutStartDate);

            //var json = new JavaScriptSerializer().Serialize(joinWorkItemsList);
            return Json(joinWorkItemsList, JsonRequestBehavior.AllowGet);
        }

        public void ConnectTFS6()
        {
            Uri tfsUri = new Uri("http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/");
            TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUri);
            //const String c_collectionUri = "https://dev.azure.com/fabrikam";
            //const String c_projectName = "MyGreatProject";
            //const String c_repoName = "MyRepo";

            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to Azure DevOps Services
            VssConnection connection = new VssConnection(tfsUri, creds);

            // Get a GitHttpClient to talk to the Git endpoints
            //GitHttpClient gitClient = connection.GetClient<GitHttpClient>();

            // Get data about a specific repository
            //var repo = gitClient.GetRepositoryAsync(c_projectName, c_repoName).Result;
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