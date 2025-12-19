using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }

    public class AddCaseLevelRequest
    {
        public long CaseHeaderId { get; set; } // you said use caseheaderid reference
        public long LevelId { get; set; }      // level id comes in input
        public string JsonData { get; set; } = "{}";
    }

    public class UpdateCaseDetailRequest
    {
        public long CaseDetailId { get; set; }
        public string JsonData { get; set; } = "{}";
        public long? LevelId { get; set; }     // optional to change level id
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
}

