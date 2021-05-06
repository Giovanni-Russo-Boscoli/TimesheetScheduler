using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimesheetScheduler.Interface
{
    public interface IUtilService
    {
        decimal FetchActiveVat();
        decimal FetchVatByDate(DateTime date);

        string FetchActiveVatText();
        string FetchVatTextByDate(DateTime date);

        double FetchRequiredHours();

        //void CaptureActiveScreen();

        //void CaptureWholeScreen();

    }
}
