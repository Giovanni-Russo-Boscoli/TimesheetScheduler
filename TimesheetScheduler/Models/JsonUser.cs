using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TimesheetScheduler.Models
{
    public class JsonUser//: IValidatableObject
    {
        private int id { get; set; }
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        private bool active { get; set; }
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;
            }
        }

        private string name { get; set; }

        [Required(ErrorMessage = "Name is Required. It cannot be empty")]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        //private string rate { get; set; }
        //public string Rate
        //{
        //    get
        //    {
        //        return rate;
        //    }
        //    set
        //    {
        //        rate = value;
        //    }
        //}
        private decimal rate { get; set; }

        //[DisplayFormat(DataFormatString = "{0:0.##}")]
        public decimal Rate
        {
            get
            {
                return rate;
            }
            set
            {
                rate = value;
            }
        }

        private bool chargeable { get; set; }
        public bool Chargeable
        {
            get
            {
                return chargeable;
            }
            set
            {
                chargeable = value;
            }
        }

        private string role { get; set; }

        [Required]
        public string Role
        {
            get
            {
                return role;
            }
            set
            {
                role = value;
            }
        }

        private string email { get; set; }

        [Required]
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                email = value;
            }
        }

        private string access { get; set; }
        public string Access
        {
            get
            {
                return access;
            }
            set
            {
                access = value;
            }
        }

        private string teamDivision{ get; set; }

        [Required]
        public string TeamDivision
        {
            get
            {
                return teamDivision;
            }
            set
            {
                teamDivision = value;
            }
        }

        private string projectNameTFS { get; set; }

        [Required]
        public string ProjectNameTFS
        {
            get
            {
                return projectNameTFS;
            }
            set
            {
                projectNameTFS = value;
            }
        }

        private string iterationPathTFS { get; set; }

        public string IterationPathTFS
        {
            get
            {
                return iterationPathTFS;
            }
            set
            {
                iterationPathTFS = value;
            }
        }
    }

}