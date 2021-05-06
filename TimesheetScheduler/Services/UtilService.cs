using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;
using TimesheetScheduler.Interface;

namespace TimesheetScheduler.Services
{
    public class UtilService: IUtilService
    {

        private static IReadJsonFiles _service;
        public UtilService()
        {
            _service = new ReadJsonFiles();
        }

        public double FetchRequiredHours()
        {
            double required_hours;
            var _reqHoursExists = double.TryParse(ConfigurationManager.AppSettings["required_hours"], out required_hours);
            if (_reqHoursExists)
            {
                return required_hours;
            }
            else
            {
                throw new Exception("'Required Hours' not found!");
            }
        }

        public decimal FetchActiveVat()
        {
            return _service.DeserializeReadJsonVATFile().Where(x => x.Active).FirstOrDefault().VAT;
        }

        public decimal FetchVatByDate(DateTime date)
        {
            var result = _service.DeserializeReadJsonVATFile().Where(x => date >= x.StartPeriod && (x.EndPeriod != null ? (date <= x.EndPeriod) : true)).FirstOrDefault();
            
            if (result == null)
            {
                throw new Exception("VAT not found by date: " + date.ToShortDateString());
            }
            return result.VAT;
        }

        public string FetchActiveVatText()
        {
            return _service.DeserializeReadJsonVATFile().Where(x => x.Active).FirstOrDefault().VATText;
        }

        public string FetchVatTextByDate(DateTime date)
        {
            var result = _service.DeserializeReadJsonVATFile().Where(x => date >= x.StartPeriod && (x.EndPeriod != null ? (date <= x.EndPeriod) : true)).FirstOrDefault();

            if (result == null)
            {
                throw new Exception("VAT not found by date: " + date.ToShortDateString());
            }
            return result.VATText;
        }

        //public void CaptureWholeScreen()
        //{
        //    var image = ScreenCapture.CaptureDesktop();
        //    image.Save(@"C:\temp\snippetsource.jpg", ImageFormat.Jpeg);
        //}

        //public void CaptureActiveScreen()
        //{
        //    var image = ScreenCapture.CaptureActiveWindow();
        //    image.Save(@"C:\temp\snippetsource.jpg", ImageFormat.Jpeg);
        //}
    }
}