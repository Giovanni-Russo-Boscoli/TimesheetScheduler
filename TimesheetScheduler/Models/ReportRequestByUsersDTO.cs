using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TimesheetScheduler.Interface;
using TimesheetScheduler.Services;

namespace TimesheetScheduler.Models
{
    public class ReportRequestByUsersDTO
    {

        public ReportRequestByUsersDTO()
        {
            //_utilService = new UtilService();
            _readJsonFilesService = new ReadJsonFiles();
        }

        //private static IUtilService _utilService;
        private static IReadJsonFiles _readJsonFilesService;

        public int  Month { get; set; }
        public int Year { get; set; }

        public IList<int> SelectedMembersId { get; set; }

        public IEnumerable<JsonUser> SelectedMembers {
            get {
                return _readJsonFilesService.DeserializeReadJsonUserFile().Where(x => this.SelectedMembersId.Contains(x.Id));
            }
        }
    }
}