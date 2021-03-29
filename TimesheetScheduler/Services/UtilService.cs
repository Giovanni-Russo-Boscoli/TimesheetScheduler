using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using TimesheetScheduler.Interface;

namespace TimesheetScheduler.Services
{
    public class UtilService: IUtilService
    {
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

        public decimal FetchVat()
        {
            decimal _vat_billing;
            var _vatExists = decimal.TryParse(ConfigurationManager.AppSettings["vat_billing"], out _vat_billing);
            if (_vatExists)
            {
                return _vat_billing;
            }
            else
            {
                throw new Exception("'VAT' not found!");
            }
        }
    }
}