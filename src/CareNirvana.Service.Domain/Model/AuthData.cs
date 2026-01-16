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
        public string? TreatementType { get; set; } // keeping DB spelling
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

        /// Raw JSON object/array you want in authdetail.data
        public string JsonData { get; set; } = "{}";
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

        /// If provided, replaces entire data jsonb
        public string? JsonData { get; set; }
    }

    // -------- Notes (stored inside authdetail.data jsonb) --------

    public class AuthNoteDto
    {
        public Guid NoteId { get; set; }
        public string NoteText { get; set; } = "";
        public int? NoteLevel { get; set; }
        public int? NoteType { get; set; }
        public bool? AuthAlertNote { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public class CreateAuthNoteRequest
    {
        public string? NoteText { get; set; }
        public int? NoteLevel { get; set; }
        public int? NoteType { get; set; }
        public bool? AuthAlertNote { get; set; }
    }

    public class UpdateAuthNoteRequest
    {
        public string? NoteText { get; set; }
        public int? NoteLevel { get; set; }
        public int? NoteType { get; set; }
        public bool? AuthAlertNote { get; set; }
    }

    // -------- Documents (stored inside authdetail.data jsonb) --------

    public class AuthDocumentDto
    {
        public Guid DocumentId { get; set; }
        public int? DocumentType { get; set; }
        public int? DocumentLevel { get; set; }
        public string DocumentDescription { get; set; } = "";
        public List<string> FileNames { get; set; } = new();

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public class CreateAuthDocumentRequest
    {
        public int? DocumentType { get; set; }
        public int? DocumentLevel { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }

    public class UpdateAuthDocumentRequest
    {
        public int? DocumentType { get; set; }
        public int? DocumentLevel { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }
}
