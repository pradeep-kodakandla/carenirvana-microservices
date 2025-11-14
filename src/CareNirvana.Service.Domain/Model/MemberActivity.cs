using System;


namespace CareNirvana.Service.Domain.Model
{
    public class MemberActivity
    {
        public int MemberActivityId { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public int? MemberDetailsId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ReferTo { get; set; }
        public bool? IsWorkBasket { get; set; }
        public int? QueueId { get; set; }
        public string Comment { get; set; }
        public int? StatusId { get; set; }
        public DateTime? PerformedDateTime { get; set; }
        public int? PerformedBy { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

    public class MemberActivityWorkGroup
    {
        public int MemberActivityWorkGroupId { get; set; }
        public int MemberActivityId { get; set; }
        public int WorkGroupWorkBasketId { get; set; }
        public int? GroupStatusId { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class MemberActivityWorkGroupAction
    {
        public int MemberActivityWorkGroupActionId { get; set; }
        public int MemberActivityWorkGroupId { get; set; }
        public int UserId { get; set; }
        public string ActionType { get; set; }    // "Accepted" / "Rejected"
        public DateTime? ActionOn { get; set; }
        public string Comment { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class MemberActivityRequestItem
    {
        public int MemberActivityId { get; set; }
        public int MemberActivityWorkGroupId { get; set; }
        public int WorkGroupWorkBasketId { get; set; }
        public int? MemberDetailsId { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public string Comment { get; set; }
        public int? StatusId { get; set; }

        public int RejectedCount { get; set; }
        public int[] RejectedUserIds { get; set; }
    }

    public class MemberActivityCurrentItem
    {
        public int MemberActivityId { get; set; }
        public int? MemberDetailsId { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public string Comment { get; set; }
        public int? StatusId { get; set; }
        public int? ReferTo { get; set; }
        public DateTime? PerformedDateTime { get; set; }
        public int? PerformedBy { get; set; }
    }
}
