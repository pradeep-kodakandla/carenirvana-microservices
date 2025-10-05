using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberHealthNote
    {
        // === Identity (kept stable) ===
        public long Id { get; set; }    // canonical id
        public long MemberHealthNotesId // kept for backward compatibility in callers
        {
            get => Id;
            set => Id = value;
        }

        // === Member pointer ===
        // OLD: some code paths referenced MemberId (memberhealthnotes.memberid).
        // NEW: table uses memberdetailsid. We preserve both for compatibility:
        public long? MemberDetailsId { get; set; } // maps to membernote.memberdetailsid

        [Obsolete("Use MemberDetailsId. This is maintained for backward compatibility only.")]
        public long MemberId
        {
            get => (long)(MemberDetailsId ?? 0);
            set => MemberDetailsId = value;
        }

        // === Note content ===
        public int? NoteTypeId { get; set; }       // membernote.notetypeid
        public string? Title { get; set; }         // optional (not persisted in current schema)
        public string Notes { get; set; } = string.Empty; // maps to membernote.membernotes
        public DateTime? EnteredTimestamp { get; set; }   // membernote.enteredtimestamp

        // === Flags ===
        public bool IsAlert { get; set; }                // membernote.isalert (bit(1))
        public bool IsExternal { get; set; }             // membernote.isexternal (bit(1))
        public bool DisplayInMemberPortal { get; set; }  // membernote.displayinmemberportal (bit(1))
        public bool ActiveFlag { get; set; } = true;     // membernote.activeflag

        // === Optional metadata ===
        public short? Importance { get; set; }           // not persisted; for future use
        public string? TagsJson { get; set; }            // not persisted; for future use

        // === Audit ===
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }

        // === Alerts linkage/timebox ===
        public DateTime? AlertEndDateTime { get; set; }  // membernote.alertenddatetime

        // === Cross-links ===
        public int? MemberProgramId { get; set; }        // membernote.memberprogramid
        public int? MemberActivityId { get; set; }       // membernote.memberactivityid

        public bool IsDeleted => DeletedOn.HasValue;
    }
}
