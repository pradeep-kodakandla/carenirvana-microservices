using System.Text.Json;

namespace CareNirvana.Service.Domain.Model
{
    public class AuthDetailRow
    {
        public long AuthDetailId { get; set; }
        public string AuthNumber { get; set; } = "";
        public int AuthTypeId { get; set; }
        public int MemberDetailsId { get; set; }
        public DateTime? AuthDueDate { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public string? TreatementType { get; set; }
        public string? RequestPriority { get; set; }
        public string? DataJson { get; set; }       // data::text
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
        public int? AuthClassId { get; set; }
        public int? AuthAssignedTo { get; set; }
        public int? AuthStatus { get; set; }
        public string? AuthTemplateName { get; set; }
        public string? AuthStatusText { get; set; }
        public string? CreatedByUserName { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public bool IsWorkgroupAssigned { get; set; }
        public bool IsWorkgroupPending { get; set; }

        // Optional (recommended for UI)
        public int[]? AssignedWorkgroupWorkbasketIds { get; set; }

        public long? AuthWorkgroupId { get; set; } // optional if you want to show/action later


    }

    public class CreateAuthRequest
    {
        public string AuthNumber { get; set; } = "";
        public int AuthTypeId { get; set; }
        public int MemberDetailsId { get; set; }
        public DateTime? AuthDueDate { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public string? TreatementType { get; set; }
        public int? AuthClassId { get; set; }
        public int? AuthAssignedTo { get; set; }
        public int? AuthStatus { get; set; }
        public string JsonData { get; set; } = "{}";

        public string RequestType { get; set; } = "AUTH";
        public long? AuthActivityId { get; set; }

        // Backward-compatible single
        public int? WorkgroupWorkbasketId { get; set; }

        // New multi
        public List<int>? WorkgroupWorkbasketIds { get; set; }

        public int? GroupStatusId { get; set; } // optional
    }

    public class UpdateAuthRequest
    {
        public int? AuthTypeId { get; set; }
        public DateTime? AuthDueDate { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public string? TreatementType { get; set; }
        public int? AuthClassId { get; set; }
        public int? AuthAssignedTo { get; set; }
        public int? AuthStatus { get; set; }
        public string? JsonData { get; set; }

        public string RequestType { get; set; } = "AUTH";
        public long? AuthActivityId { get; set; }

        public int? WorkgroupWorkbasketId { get; set; }
        public List<int>? WorkgroupWorkbasketIds { get; set; }

        public int? GroupStatusId { get; set; } // optional
    }


    // -------- Notes (stored inside authdetail.data jsonb) --------

    public sealed class AuthNoteDto
    {
        public Guid NoteId { get; set; }
        public string NoteText { get; set; } = "";

        public int? NoteType { get; set; }       // from UM: authorizationNoteType
        public bool? AuthAlertNote { get; set; } = false;// from UM: authorizationAlertNote
        public DateTime? EncounteredOn { get; set; }  // from UM: noteEncounteredDatetime
        public DateTime? AlertEndDate { get; set; }   // from UM: newDate_copy_q5d60fyd5

        public long CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }


    public sealed class CreateAuthNoteRequest
    {
        public string? NoteText { get; set; }
        public int? NoteType { get; set; }
        public bool? AuthAlertNote { get; set; } = false;
        public DateTime? EncounteredOn { get; set; }
        public DateTime? AlertEndDate { get; set; }
    }


    public sealed class UpdateAuthNoteRequest
    {
        public string? NoteText { get; set; }
        public int? NoteType { get; set; }
        public bool? AuthAlertNote { get; set; }
        public DateTime? EncounteredOn { get; set; }
        public DateTime? AlertEndDate { get; set; }
    }


    // -------- Documents (stored inside authdetail.data jsonb) --------

    public sealed class AuthDocumentDto
    {
        public Guid DocumentId { get; set; }

        public int? DocumentType { get; set; }              // authorizationDocumentType
        public string? DocumentDescription { get; set; }    // authorizationDocumentDesc
        public List<string> FileNames { get; set; } = new();

        public long CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public long? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public sealed class CreateAuthDocumentRequest
    {
        public int? DocumentType { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }


    public sealed class UpdateAuthDocumentRequest
    {
        public int? DocumentType { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }


    public sealed class TemplateSectionsResponse
    {
        public int CaseTemplateId { get; set; }
        public string GroupName { get; set; } = "";
        public JsonElement Sections { get; set; } // JSON array
    }

    public sealed class TemplateSectionResponse
    {
        public int CaseTemplateId { get; set; }
        public string SectionName { get; set; } = "";
        public JsonElement Section { get; set; }
    }


    public sealed class DecisionSectionItemDto
    {
        public Guid ItemId { get; set; }
        public JsonElement Data { get; set; }

        public long CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public long? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public sealed class CreateDecisionSectionItemRequest
    {
        public JsonElement Data { get; set; } // entire section payload as json
    }

    public sealed class UpdateDecisionSectionItemRequest
    {
        public JsonElement? Data { get; set; } // partial/replace (you choose)
    }

    public sealed class AuthWorkgroupRow
    {
        public long AuthWorkgroupId { get; set; }
        public string RequestType { get; set; } = "AUTH"; // AUTH | ACTIVITY
        public long AuthDetailId { get; set; }
        public long? AuthActivityId { get; set; }
        public int WorkgroupWorkbasketId { get; set; }
        public int? GroupStatusId { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public sealed class AuthWorkgroupActionRow
    {
        public long AuthWorkgroupActionId { get; set; }
        public long AuthWorkgroupId { get; set; }
        public int UserId { get; set; }          // user who accepted/rejected
        public string ActionType { get; set; } = ""; // ACCEPT | REJECT (or your enum)
        public DateTime ActionOn { get; set; }
        public string? Comment { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public sealed class SaveAuthWorkgroupRequest
    {
        public string RequestType { get; set; } = "AUTH"; // AUTH | ACTIVITY
        public long AuthDetailId { get; set; }
        public long? AuthActivityId { get; set; }

        // If null => means "not selected" and we should fallback assignment behavior
        public int? WorkgroupWorkbasketId { get; set; }
        public int? GroupStatusId { get; set; }

        // optional action logging
        public string? ActionType { get; set; }  // ACCEPT | REJECT
        public string? Comment { get; set; }
    }

    public sealed class SaveAuthWorkgroupsRequest
    {
        public string RequestType { get; set; } = "AUTH"; // AUTH | ACTIVITY
        public long AuthDetailId { get; set; }
        public long? AuthActivityId { get; set; }         // required for ACTIVITY
        public int[] WorkgroupWorkbasketIds { get; set; } = Array.Empty<int>();
        public int? GroupStatusId { get; set; }           // optional (you can set on insert)
    }


}
