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
        public string? ReferredTo { get; set; }
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
    }
}
