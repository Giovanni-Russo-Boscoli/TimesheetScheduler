using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Services;

namespace TimesheetScheduler.Models
{
    public class ConsolidatedMonthUserData
    {

        private static IReadJsonFiles _readJsonFilesService;
        private static IUtilService _utilService;


        public ConsolidatedMonthUserData()
        {
            ListWorkItem = new List<WorkItemSerialized>();
            ChargeableHours = 0;
            NonChargeableHours = 0;
            //TotalHours = 0;
            _readJsonFilesService = new ReadJsonFiles();
            _utilService = new UtilService();
            Vat_Fee = _utilService.FetchVat();
        }

        private string userName { get; set; }
        public string UserName {
            get {
                return userName;
            }
            set {
                userName = value.Replace("''", "'");
            }
        }

        public decimal? Vat_Fee
        {
            get;internal set;
        }

        public IList<WorkItemSerialized> ListWorkItem { get; set; }

        public double WorkedDays
        {
            get
            {
                double _requiredHours;
                double.TryParse(ConfigurationManager.AppSettings["required_hours"], out _requiredHours);
                return Math.Round((this.ChargeableHours / _requiredHours), 2);
            }
        }

        public double TotalHours
        {
            get
            {
                return Math.Round((this.ChargeableHours + this.NonChargeableHours), 2);
            }
        }

        public double ChargeableHours
        {
            get; set;
        }

        public double NonChargeableHours
        {
            get; set;
        }

        public decimal RateExcludingVAT
        {
            get
            {
                return _readJsonFilesService.GetMemberRate(this.UserName);
            }
        }

        public decimal RateIncludingVAT
        {
            get
            {
                var _rateExcludingVAT = this.RateExcludingVAT;
                return Math.Round((_rateExcludingVAT + (_rateExcludingVAT * (Vat_Fee.Value / 100))), 2);
            }
        }

        private decimal _totalExcludingVAT = -1;

        public decimal TotalExcludingVAT
        {
            get { return Math.Round(_totalExcludingVAT,2); }
            set
            {
                _totalExcludingVAT = value;
            }
        }

        private decimal _totalIncludingVAT = -1;
        public decimal TotalIncludingVAT
        {
            get { return Math.Round(_totalIncludingVAT, 2); }
            set
            {
                _totalIncludingVAT = value;
            }
        }

    }
}