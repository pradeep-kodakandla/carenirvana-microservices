using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class AuthActivity
    {
        public int AuthActivityId { get; set; }
        public int? AuthDetailId { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? PriorityId { get; set; }
        public int? ProviderId { get; set; }
        public DateTime? FollowUpDateTime { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ReferredTo { get; set; }
        public bool? IsWorkBasket { get; set; }
        public int? QueueId { get; set; }
        public string? Comment { get; set; }
        public int? StatusId { get; set; }
        public DateTime? PerformedDateTime { get; set; }
        public int? PerformedBy { get; set; }
        public bool? ActiveFlag { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }

        public int? ServiceLineCount { get; set; }
        public string? MdReviewStatus { get; set; }
        public string? MdAggregateDecision { get; set; }
        public string? PayloadSnapshotJson { get; set; }
    }


    public sealed class MdReviewActivityCreate
    {
        public AuthActivity Activity { get; set; } = default!;
        public IReadOnlyList<MdReviewActivityLineCreate> Lines { get; set; } = Array.Empty<MdReviewActivityLineCreate>();
        public string? PayloadSnapshotJson { get; set; } // optional audit snapshot of selected lines
    }

    public sealed class MdReviewActivityLineCreate
    {
        public long? DecisionLineId { get; set; }
        public string? ServiceCode { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public int? Requested { get; set; }
        public int? Approved { get; set; }
        public int? Denied { get; set; }
        public string? InitialRecommendation { get; set; } // "Approved" | "Denied" | "Pending"
    }
}
