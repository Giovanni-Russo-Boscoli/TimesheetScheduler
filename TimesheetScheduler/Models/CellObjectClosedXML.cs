namespace TimesheetScheduler.Models
{
    public class CellObjectClosedXML
    {
        public string CellPosition { get; set; }
        public string CellValue { get; set; }
        public string CellFormula { get; set; }
        public ParamsFormatCellClosedXML FormatParams { get; set; }
    }
}