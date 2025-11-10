using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberAlertView
    {
        public int MemberAlertId { get; set; }
        public int? MemberDetailsId { get; set; }
        public string? MemberFirstName { get; set; }
        public string? MemberLastName { get; set; }
        public int? AlertId { get; set; }
        public string? CfgAlertName { get; set; }

        public int? AlertSourceId { get; set; }
        public string? AlertSourceName { get; set; }
        public string? AlertSourceCode { get; set; }

        public int? AlertTypeId { get; set; }
        public string? AlertTypeName { get; set; }
        public string? AlertTypeCode { get; set; }

        public int? AlertStatusId { get; set; }
        public string? AlertStatusName { get; set; }
        public string? AlertStatusCode { get; set; }

        public DateTime? AlertDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DismissedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }

        // For paging (COUNT(*) OVER())
        public long TotalCount { get; set; }
    }

    public sealed class MemberAlertPagedResult
    {
        public long Total { get; set; }
        public IReadOnlyList<MemberAlertView> Items { get; set; } = Array.Empty<MemberAlertView>();
    }

    public sealed class UpdateAlertStatusDto
    {
        public int? AlertStatusId { get; set; }
        public DateTime? DismissedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

}
