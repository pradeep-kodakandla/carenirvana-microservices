using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberCareStaff
    {
        public int MemberCareStaffId { get; set; }
        public int? UserId { get; set; }
        public int? MemberDetailsId { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    // Read model with joined names
    public class MemberCareStaffView : MemberCareStaff
    {
        public string? UserName { get; set; }              // from securityuserdetail.username
        public string? UserFirstName { get; set; }         // optional if you store first/last
        public string? UserLastName { get; set; }

        public string? MemberFirstName { get; set; }       // from memberdetails.firstname
        public string? MemberLastName { get; set; }        // from memberdetails.lastname
    }

    public class MemberCareStaffCreateRequest
    {
        public int UserId { get; set; }
        public int MemberDetailsId { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class MemberCareStaffUpdateRequest
    {
        public int? UserId { get; set; }
        public int? MemberDetailsId { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class PagedResult<T>
    {
        public int Total { get; set; }
        public T[] Items { get; set; } = Array.Empty<T>();
    }
}
