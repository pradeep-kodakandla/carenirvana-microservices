using CareNirvana.Domain.MemberJourney;
using System;


namespace CareNirvana.Service.Domain.Model
{
    public class MemberJourney
    {
        public class MemberJourneyEvent
        {
            public string EventId { get; set; } = default!;            // e.g., "authdetail:12345"
            public long MemberDetailsId { get; set; }
            public EventCategory Category { get; set; }
            public string Title { get; set; } = default!;              // e.g., "Claim Processed: $500 Outpatient"
            public string? Subtitle { get; set; }                      // small helper text (e.g., "Visit for routine consultation")
            public string? Severity { get; set; }                      // High/Medium/Low… (optional)
            public DateTime EventUtc { get; set; }                     // the point on the timeline
            public string? Icon { get; set; }                          // optional icon key your UI can map
            public string? SourceId { get; set; }                      // raw PK from source table
            public string? SourceTable { get; set; }                   // "authdetail", "membernote", etc.
            public string? ActionUrl { get; set; }                     // optional deep-link
            public string? ExtraJson { get; set; }                     // table-specific payload (serialized JSON)
        }
        public class MemberJourneySummary
        {
            public int Total { get; set; }
            public int AuthCount { get; set; }
            public int AuthActivityCount { get; set; }
            public int EnrollmentCount { get; set; }
            public int CareStaffCount { get; set; }
            public int CaregiverCount { get; set; }
            public int ProgramCount { get; set; }
            public int NoteCount { get; set; }
            public int RiskCount { get; set; }   // NEW
            public int AlertCount { get; set; }  // NEW
        }

        public class MemberJourneyRequest
        {
            public long MemberDetailsId { get; set; }
            public DateTime? FromUtc { get; set; }      // default: last 30 days
            public DateTime? ToUtc { get; set; }        // default: now
            public int Page { get; set; } = 1;          // 1-based
            public int PageSize { get; set; } = 25;     // UI can pass 25/50 etc.
            public string? Search { get; set; }         // free text search
            public IReadOnlyCollection<EventCategory>? Categories { get; set; } // filter by categories
        }

        public class PagedResult<T>
        {
            public IReadOnlyList<T> Items { get; set; } = new List<T>();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int Total { get; set; }
        }
    }
}

namespace CareNirvana.Domain.MemberJourney
{
    public enum EventCategory
    {
        Auth = 1,
        AuthActivity = 2,
        Enrollment = 3,
        CareStaff = 4,
        Caregiver = 5,
        Program = 6,
        Note = 7,
        Risk = 8,
        Alert = 9
    }
}