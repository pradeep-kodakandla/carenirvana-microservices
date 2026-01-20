using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class CaseHeader
    {
        public long CaseHeaderId { get; set; }
        public string CaseNumber { get; set; } = "";
        public string CaseType { get; set; } = "";
        public string Status { get; set; } = "";

        public long? MemberDetailId { get; set; }

        // NEW (from joins)
        public string? CreatedByUserName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }

        public DateTime CreatedOn { get; set; }
        public long CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }
    }


    public class CaseDetail
    {
        public long CaseDetailId { get; set; }

        public long CaseHeaderId { get; set; }
        public long CaseLevelId { get; set; }              // input “level1 id / levelid”
        public string CaseLevelNumber { get; set; } = "";  // ex: 51471L1, 51471L2

        public string JsonData { get; set; } = "{}";

        public DateTime CreatedOn { get; set; }
        public long CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }

        public DateTime? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }

        public bool IsWorkgroupAssigned { get; set; }
        public bool IsWorkgroupPending { get; set; }
        public int[]? AssignedWorkgroupWorkbasketIds { get; set; }

    }

    public class CaseAggregate
    {
        public CaseHeader Header { get; set; } = new();
        public List<CaseDetail> Details { get; set; } = new();
    }


    public class CreateCaseRequest
    {
        public string CaseNumber { get; set; } = "";
        public string CaseType { get; set; } = "";
        public string Status { get; set; } = "";
        public long? MemberDetailId { get; set; }
        public long LevelId { get; set; }      // Level 1 id comes in input
        public string JsonData { get; set; } = "{}";

        // Workgroup assignment (optional)
        public int? WorkgroupWorkbasketId { get; set; }                 // single (backward compatible)
        public List<int>? WorkgroupWorkbasketIds { get; set; }          // multi
        public int? GroupStatusId { get; set; }

    }

    public class AddCaseLevelRequest
    {
        public long CaseHeaderId { get; set; } // you said use caseheaderid reference
        public long LevelId { get; set; }      // level id comes in input
        public string JsonData { get; set; } = "{}";

        // Workgroup assignment (optional)
        public int? WorkgroupWorkbasketId { get; set; }                 // single (backward compatible)
        public List<int>? WorkgroupWorkbasketIds { get; set; }          // multi
        public int? GroupStatusId { get; set; }

    }

    public class UpdateCaseDetailRequest
    {
        public long CaseDetailId { get; set; }
        public string JsonData { get; set; } = "{}";
        public long? LevelId { get; set; }     // optional to change level id

        // Workgroup assignment (optional)
        public int? WorkgroupWorkbasketId { get; set; }                 // single (backward compatible)
        public List<int>? WorkgroupWorkbasketIds { get; set; }          // multi
        public int? GroupStatusId { get; set; }

    }

    public class CreateCaseResult
    {
        public long CaseHeaderId { get; set; }
        public string CaseNumber { get; set; } = "";
        public long CaseDetailId { get; set; }

        public string CaseLevelNumber { get; set; } = "";
    }

    public class AddLevelResult
    {
        public long CaseHeaderId { get; set; }
        public string CaseNumber { get; set; } = "";
        public long CaseDetailId { get; set; }
        public string CaseLevelNumber { get; set; } = "";
    }

    public sealed class CaseNoteDto
    {
        public Guid NoteId { get; set; }

        public string NoteText { get; set; } = "";

        public int NoteLevel { get; set; }           // int
        public int NoteType { get; set; }            // int
        public bool CaseAlertNote { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public sealed class CreateCaseNoteRequest
    {
        public string NoteText { get; set; } = "";
        public int NoteLevel { get; set; }           // int
        public int NoteType { get; set; }            // int
        public bool CaseAlertNote { get; set; }
    }

    public sealed class UpdateCaseNoteRequest
    {
        // Patch-style: only update what is non-null
        public string? NoteText { get; set; }
        public int? NoteLevel { get; set; }          // int?
        public int? NoteType { get; set; }           // int?
        public bool? CaseAlertNote { get; set; }     // bool?
    }

    public sealed class CaseNotesResponse
    {
        public int CaseHeaderId { get; set; }
        public int LevelId { get; set; }
        public List<CaseNoteDto> Notes { get; set; } = new();
    }

    public sealed class CaseNotesTemplateResponse
    {
        public int CaseTemplateId { get; set; }
        public string SectionName { get; set; } = "Case Notes";

        // Raw JSON for the section (you can deserialize later into your TemplateSection model)
        public JsonElement Section { get; set; }
    }


    public sealed class CaseDocumentsTemplateResponse
    {
        public int CaseTemplateId { get; set; }
        public string SectionName { get; set; } = "Case Documents";
        public System.Text.Json.JsonElement Section { get; set; }
    }

    public sealed class CaseDocumentsResponse
    {
        public int CaseHeaderId { get; set; }
        public int LevelId { get; set; }
        public List<CaseDocumentDto> Documents { get; set; } = new();
    }

    public sealed class CaseDocumentDto
    {
        public Guid DocumentId { get; set; }
        public int DocumentType { get; set; }      // from master later
        public int DocumentLevel { get; set; }     // from master later
        public string DocumentDescription { get; set; } = "";

        public List<string> FileNames { get; set; } = new();

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

    public sealed class CaseDocumentFileDto
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long Size { get; set; }
    }

    public sealed class CreateCaseDocumentRequest
    {
        public int DocumentType { get; set; }
        public int DocumentLevel { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }

    public sealed class UpdateCaseDocumentRequest
    {
        public int? DocumentType { get; set; }
        public int? DocumentLevel { get; set; }
        public string? DocumentDescription { get; set; }
        public List<string>? FileNames { get; set; }
    }

}

