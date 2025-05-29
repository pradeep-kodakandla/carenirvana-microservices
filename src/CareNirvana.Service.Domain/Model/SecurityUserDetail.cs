using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class SecurityUserDetail
    {
        public int UserDetailId { get; set; }

        public string Title { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string MiddleName { get; set; }

        public string Suffix { get; set; }

        public string Credentials { get; set; }

        public CfgSecurityRole? Role { get; set; }

        public DateTime DateOfStarting { get; set; }

        public string ClinicName { get; set; }

        public string Speciality { get; set; }

        public CfgTimeZone? TimeZone { get; set; }

        public CfgRateType? RateType { get; set; }

        public Decimal Rate { get; set; }

        public int ManagerId { get; set; }

        public string PrimaryEmail { get; set; }

        public string AlternateEmail { get; set; }

        public string PrimaryPhone { get; set; }

        public string PrimayPhoneExtension { get; set; }

        public string AlternatePhone { get; set; }

        public string AlternatePhoneExtension { get; set; }

        public string MobilePhone { get; set; }

        public byte[] Signature { get; set; }

        public bool IsLocked { get; set; }

        public bool ActiveFlag { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }
}

