using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public sealed class CaseActivity
    {
        public int CaseActivityId { get; set; }
        public int CaseHeaderId { get; set; }
        public int MemberDetailsId { get; set; }
        public int CaseLevelId { get; set; }

        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }

        public int? ReferTo { get; set; }           
        public bool? IsWorkBasket { get; set; }
        public int? QueueId { get; set; }
        public string? Comment { get; set; }
        public int? StatusId { get; set; }

        public bool ActiveFlag { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    public sealed class CaseActivityRowDto
    {
        public int CaseActivityId { get; set; }
        public int CaseHeaderId { get; set; }
        public int MemberDetailsId { get; set; }
        public int CaseLevelId { get; set; }

        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ReferTo { get; set; }
        public string? Comment { get; set; }

        // computed
        public string RequestStatus { get; set; } = "OPEN"; // OPEN/REQUESTED/ACCEPTED/REJECTED
    }

    public sealed class CaseActivityCreateDto
    {
        public int CaseHeaderId { get; set; }
        public int MemberDetailsId { get; set; }
        public int CaseLevelId { get; set; }

        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Comment { get; set; }
        public int? StatusId { get; set; }

        // If group request:
        public bool IsGroupRequest { get; set; } = false;

        // Target workgroup baskets to request (one activity can go to multiple)
        public List<int>? WorkGroupWorkBasketIds { get; set; }

        public int CreatedBy { get; set; }
    }

    public sealed class CaseActivityUpdateDto
    {
        public int CaseActivityId { get; set; }

        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Comment { get; set; }
        public int? StatusId { get; set; }

        public int UpdatedBy { get; set; }
    }

    public sealed class WorkgroupActionDto
    {
        public int CaseWorkgroupId { get; set; }
        public int UserId { get; set; }
        public int CaseLevelId { get; set; }  // you wanted caselevelid in all tables
        public string? Comment { get; set; }
    }

    public sealed class CaseActivityTemplateResponse
    {
        public int CaseTemplateId { get; set; }
        public string SectionName { get; set; } = "Case Notes";

        // Raw JSON for the section (you can deserialize later into your TemplateSection model)
        public JsonElement Section { get; set; }
    }

}
