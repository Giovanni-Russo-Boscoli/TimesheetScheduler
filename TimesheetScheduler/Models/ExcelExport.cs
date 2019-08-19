using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class ExcelExport
    {
        public string UserName { get; set; }

        public string MonthTimesheet { get; set; }

        public string YearTimesheet { get; set; }

        public string TitleTimesheet()
        {
            var titleTimesheet = string.Empty;
            titleTimesheet = "Timesheet_" + NameSplittedByUnderscore(UserName) + "_" + MonthTimesheet + "_" + YearTimesheet;
            return titleTimesheet;
        }

        #region Const Cell Range and Labels

        public string CellRangeProjectNameLabel { get; set; }
        public string ValueProjectNameLabel { get; set; }
        public string CellRangeProjectNameValue { get; set; }

        #endregion Const Cell Range and Labels

        public string NameSplittedByUnderscore(string name)
        {            
            return name.Replace(" ", "_");
        }        

        public void InitExcelVariables()
        {
            UserName = "GIOVANNI RUSSO BOSCOLI";
            MonthTimesheet = DateTime.Now.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            YearTimesheet = DateTime.Now.Year.ToString();
            CellRangeProjectNameLabel = "B1";
            ValueProjectNameLabel = "Project Name";
            CellRangeProjectNameValue = "C1";
        }        

        public void ExcelFileExport()
        {
            Application excel;
            Workbook worKbooK;
            Worksheet worksheet;
            Microsoft.Office.Interop.Excel.Range celLrangE;

            try
            {
                excel = new Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);
                InitExcelVariables();
                worksheet = (Worksheet)worKbooK.ActiveSheet;
                worksheet.Name = "TIMESHEET";
                //worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 8]].Merge();
                //worksheet.Cells[1, 1] = "Student Report Card";
                SetCellValue(worksheet, CellRangeProjectNameLabel, ValueProjectNameLabel);
                FormatCell(worksheet, CellRangeProjectNameLabel,
                    new ParamsFormatCell()
                    {
                        fontBold = true,
                        fontSize = 25,
                        fontItalic = true,
                        fontColor = Color.Yellow,
                        cellBackgroundColor = Color.Green
                    }
               );
                SetCellValue(worksheet, CellRangeProjectNameValue, "BOMi Modelling Team");
                int rowcount = 2;
                foreach (DataRow datarow in TimesheetTable().Rows)
                {
                    rowcount += 1;
                    for (int i = 1; i <= TimesheetTable().Columns.Count; i++)
                    {
                        if (rowcount == 3)
                        {
                            worksheet.Cells[2, i] = TimesheetTable().Columns[i - 1].ColumnName;
                            //worksheet.Cells.Font.Color = System.Drawing.Color.Black;          
                        }
                        worksheet.Cells[rowcount, i] = datarow[i - 1].ToString();
                        if (rowcount > 3)
                        {
                            if (i == TimesheetTable().Columns.Count)
                            {
                                if (rowcount % 2 == 0)
                                {
                                    celLrangE = worksheet.Range[worksheet.Cells[rowcount, 1], worksheet.Cells[rowcount, TimesheetTable().Columns.Count]];
                                }
                            }
                        }
                    }
                }

                celLrangE = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[rowcount, TimesheetTable().Columns.Count]];
                celLrangE.EntireColumn.AutoFit();
                Borders border = celLrangE.Borders;
                border.LineStyle =XlLineStyle.xlContinuous;
                border.Weight = 2d;
                //worksheet.Cells.Font.Size = 27;
                celLrangE = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[2, TimesheetTable().Columns.Count]];
                ////welfare.irlgov.ie/shares/FRDIRWIN7_FI/giovanniboscoli/Desktop/
                worKbooK.SaveAs(TitleTimesheet());
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

        public System.Data.DataTable TimesheetTable()
        {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Sex", typeof(string));
            table.Columns.Add("Subject1", typeof(int));
            table.Columns.Add("Subject2", typeof(int));
            table.Columns.Add("Subject3", typeof(int));
            table.Columns.Add("Subject4", typeof(int));
            table.Columns.Add("Subject5", typeof(int));
            table.Columns.Add("Subject6", typeof(int));
            table.Rows.Add(1, "Amar", "M", 78, 59, 72, 95, 83, 77);
            table.Rows.Add(2, "Mohit", "M", 76, 65, 85, 87, 72, 90);
            table.Rows.Add(3, "Garima", "F", 77, 73, 83, 64, 86, 63);
            table.Rows.Add(4, "jyoti", "F", 55, 77, 85, 69, 70, 86);
            table.Rows.Add(5, "Avinash", "M", 87, 73, 69, 75, 67, 81);
            table.Rows.Add(6, "Devesh", "M", 92, 87, 78, 73, 75, 72);
            return table;
        }

        public void SetCellValue(Worksheet worksheet, string cellRange, string value)
        {
            worksheet.get_Range(cellRange).Value = value;
        }
        public void FormatCell(Worksheet worksheet, string cellRange, ParamsFormatCell paramsCell)
        {
            //https://docs.microsoft.com/en-us/visualstudio/vsto/how-to-programmatically-apply-color-to-excel-ranges?view=vs-2019
            worksheet.get_Range(cellRange).Font.Size = paramsCell.fontSize;
            //worksheet.get_Range(cellRange).Font.Family = paramsCell.fontSize;
            worksheet.get_Range(cellRange).Font.Bold = paramsCell.fontBold;
            worksheet.get_Range(cellRange).Font.Italic = paramsCell.fontItalic;
            worksheet.get_Range(cellRange).Font.Color = paramsCell.fontColor;
            worksheet.get_Range(cellRange).Interior.Color = paramsCell.cellBackgroundColor;
        }
        public class ParamsFormatCell
        {
            public int fontSize { get; set; }// = 11;
            public string fontFamily { get; set; }
            public bool fontBold { get; set; }// = false;
            public bool fontItalic { get; set; } //= false;
            public Color fontColor { get; set; } // = System.Drawing.Color.Black;
            public Color cellBackgroundColor { get; set; } // = System.Drawing.Color.Black;
        }

    }
}