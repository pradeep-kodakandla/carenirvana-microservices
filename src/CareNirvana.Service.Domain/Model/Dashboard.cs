using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    internal class Dashboard
    {
    }
    public class MemberSummary
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? MemberId { get; set; }
        public int? MemberDetailsId { get; set; }

        public decimal? RiskScore { get; set; }
        public int? RiskLevelId { get; set; }
        public string? RiskLevelCode { get; set; }

        public DateTime? LastContact { get; set; }   // currently NULL in SQL (reserved)
        public DateTime? NextContact { get; set; }   // currently NULL in SQL (reserved)

        public string? City { get; set; }
        public string? MemberPhoneNumberId { get; set; }

        public string? LevelMap { get; set; }        // JSON string
        public int AuthCount { get; set; }           // coalesce -> non-null
        public string? DOB { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EnrollmentEndDate { get; set; }
        public string? Gender { get; set; }
        public string? Programs { get; set; }
        public int ActivityCount { get; set; }
        public int ComplaintCount { get; set; }
        public int CarePlanCount { get; set; }

        public int AlertCount { get; set; }
    }

    public sealed class AuthDetailListItem
    {
        public string AuthNumber { get; set; } = "";
        public int? AuthStatus { get; set; }
        public string? AuthStatusValue { get; set; }
        public string? TemplateName { get; set; }
        public string? AuthClassValue { get; set; }

        public int MemberId { get; set; }
        public int? MemberDetailsId { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public DateTime? AuthDueDate { get; set; }

        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedUser { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        // Header-level (from Auth Details -> entries[0])
        public string? TreatmentType { get; set; }          // raw id/text from JSON (e.g., "3")
        public string? TreatmentTypeValue { get; set; }     // display value from cfgadmindata
        public string? AuthPriority { get; set; }           // raw id/text from JSON (e.g., "1")
        public string? RequestPriorityValue { get; set; }   // display value from cfgadmindata
        public string? MemberName { get; set; }
    }

    public class ActivityItem
    {
        public string Module { get; set; }                 // 'UM'
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? MemberId { get; set; }
        public int? MemberDetailsId { get; set; }
        public DateTime? CreatedOn { get; set; }

        public int? ActivityTypeId { get; set; }
        public string ActivityType { get; set; }

        public int? ReferredTo { get; set; }
        public string UserName { get; set; }

        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }

        public int? StatusId { get; set; }
        public string Status { get; set; }                 // 'Pending'
        public string? Comments { get; set; }
        public string? AuthNumber { get; set; }
        public string? AuthActivityId { get; set; }
    }

    public class ActivityRequestItem : ActivityItem
    {
        public int RejectedCount { get; set; }
        public int[] RejectedUserIds { get; set; } = Array.Empty<int>();
        public int WorkGroupId { get; set; }
        public string WorkGroupName { get; set; }
        public int WorkBasketId { get; set; }
        public string WorkBasketName { get; set; }
        public int MemberActivityWorkGroupId { get; set; }
    }

    public class UserWorkGroupWorkBasketItem
    {
        public int UserId { get; set; }
        public string UserFullName { get; set; }

        public int WorkGroupWorkBasketId { get; set; }

        public int WorkGroupId { get; set; }
        public string WorkGroupName { get; set; }

        public int WorkBasketId { get; set; }
        public string WorkBasketName { get; set; }

        public bool ActiveFlag { get; set; }

        public int[] AssignedUserIds { get; set; } = Array.Empty<int>();
        public string[] AssignedUserNames { get; set; } = Array.Empty<string>();
    }

    public class AuthActivityLine
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }                     // FK -> AuthActivity.AuthActivityId
        public int? DecisionLineId { get; set; }                // nullable
        public string ServiceCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int? Requested { get; set; }
        public int? Approved { get; set; }
        public int? Denied { get; set; }

        public string? InitialRecommendation { get; set; }      // e.g., "Approved/Denied/Partial" or empty
        public string Status { get; set; } = string.Empty;       // e.g., "Pending"
        public string MdDecision { get; set; } = string.Empty;   // e.g., "NotReviewed/Approved/Denied"
        public string? MdNotes { get; set; }

        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? Version { get; set; }
        public string? Comments { get; set; }
    }

    public class FaxFile
    {
        public long FaxId { get; set; }
        public string FileName { get; set; } = "";   // DB: filename
        public string Url { get; set; } = "";   // DB: storedpath

        // New metadata
        public string? OriginalName { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public string? Sha256Hex { get; set; }
        public int? UploadedBy { get; set; }
        public DateTimeOffset? UploadedAt { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }
        public int PageCount { get; set; } = 1;
        public long? MemberId { get; set; }
        public string? WorkBasket { get; set; }
        public short Priority { get; set; } = 2;    // 1=High,2=Normal,3=Low
        public string Status { get; set; } = "New";

        // Processing & OCR
        public string ProcessStatus { get; set; } = "Pending"; // Pending|Processing|Ready|Failed
        public string? OcrText { get; set; }
        public string? OcrJsonPath { get; set; }

        // JSONB meta (store as raw JSON string; keep it flexible)
        public string? MetaJson { get; set; }
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public int? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

}
