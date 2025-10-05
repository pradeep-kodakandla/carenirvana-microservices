using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    // Models/MemberCaregiver.cs
    public class MemberCaregiver
    {
        public int MemberCaregiverId { get; set; }
        public int? MemberDetailsId { get; set; }
        public string? CaregiverFirstName { get; set; }
        public string? CaregiverLastName { get; set; }
        public string? CaregiverMiddleName { get; set; }
        public DateTime? CaregiverBrithDate { get; set; }   // (typo kept to match column)
        public int? GenderId { get; set; }
        public int? EthnicityId { get; set; }
        public int? RaceId { get; set; }
        public int? ResidenceStatusId { get; set; }
        public int? MaritalStatusId { get; set; }
        public int? RelationshipTypeId { get; set; }
        public string? PrimaryEmail { get; set; }
        public string? AlternateEmail { get; set; }
        public bool? IsHealthcareProxy { get; set; } // bit(1)
        public bool? IsPrimary { get; set; }         // bit(1)
        public bool? IsFormalCaregiver { get; set; } // bit(1)
        public bool ActiveFlag { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Models/MemberCaregiverAddress.cs
    public class MemberCaregiverAddress
    {
        public int MemberCaregiverAddressId { get; set; }
        public int? MemberCaregiverId { get; set; }
        public int? AddressTypeId { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? City { get; set; }
        public int? CountyId { get; set; }
        public int? StateId { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public int? BoroughId { get; set; }
        public int? IslandId { get; set; }
        public int? RegionId { get; set; }
        public bool? IsPrimary { get; set; } // bit(1)
        public bool ActiveFlag { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Models/MemberCaregiverLanguage.cs
    public class MemberCaregiverLanguage
    {
        public int MemberCaregiverLanguageId { get; set; }
        public int? MemberCaregiverId { get; set; }
        public int? LanguageId { get; set; }
        public bool? IsPrimary { get; set; } // bit(1)
        public bool ActiveFlag { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Models/MemberCaregiverMemberPortal.cs
    public class MemberCaregiverMemberPortal
    {
        public int MemberCaregiverMemberPortalId { get; set; }
        public int? MemberCaregiverId { get; set; }
        public bool? IsMemberPortalAccess { get; set; }   // bit(1)
        public bool? IsRegistrationRequired { get; set; } // bit(1)
        public bool ActiveFlag { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Models/MemberCaregiverPhone.cs
    public class MemberCaregiverPhone
    {
        public int MemberCaregiverPhoneId { get; set; }
        public int? MemberCaregiverId { get; set; }
        public int? PhoneTypeId { get; set; }
        public bool? IsPrimary { get; set; } // bit(1)
        public bool ActiveFlag { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Models/DTOs/MemberCaregiverDto.cs
    public class MemberCaregiverDto
    {
        public MemberCaregiver Caregiver { get; set; } = new();
        public List<MemberCaregiverAddress> Addresses { get; set; } = new();
        public List<MemberCaregiverPhone> Phones { get; set; } = new();
        public List<MemberCaregiverLanguage> Languages { get; set; } = new();
        public List<MemberCaregiverMemberPortal> Portal { get; set; } = new();
    }

}
