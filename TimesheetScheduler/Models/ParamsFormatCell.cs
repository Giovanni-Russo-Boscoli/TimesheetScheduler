using Microsoft.Office.Interop.Excel;
using System.Drawing;

namespace TimesheetScheduler.Models
{
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
}