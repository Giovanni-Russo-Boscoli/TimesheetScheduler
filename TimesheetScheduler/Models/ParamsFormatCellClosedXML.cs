using ClosedXML.Excel;

namespace TimesheetScheduler.Models
{
    public class ParamsFormatCellClosedXML
    {
        public int fontSize { get; set; } = 11;
        public string fontFamily { get; set; }
        public bool fontBold { get; set; } = false;
        public bool fontItalic { get; set; } = false;
        public XLColor fontColor { get; set; } = XLColor.Black;
        public XLColor cellBackgroundColor { get; set; } = XLColor.Transparent;
        public XLAlignmentVerticalValues aligmentVertical { get; set; } = XLAlignmentVerticalValues.Center;
        public XLAlignmentHorizontalValues aligmentHorizontal { get; set; } = XLAlignmentHorizontalValues.Left;
        public bool lockCell { get; set; } = false;

        //public XlLineStyle borderLineStyle { get; set; }
        //public Color borderColor { get; set; }
    }
}