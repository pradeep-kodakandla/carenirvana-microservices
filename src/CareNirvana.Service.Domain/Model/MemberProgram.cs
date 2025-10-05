using System;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberProgram
    {
        public int MemberProgramId { get; set; }
        public int MemberDetailsId { get; set; }
        public int ProgramId { get; set; }
        public int? MemberEnrollmentId { get; set; }
        public int ProgramStatusId { get; set; }
        public int? ProgramStatusReasonId { get; set; }
        public int? ProgramReferralSourceId { get; set; }
        public int? AssignedTo { get; set; }
        public DateTime StartDate { get; set; }            // DATE in DB
        public DateTime? EndDate { get; set; }             // DATE in DB
        public bool? ActiveFlag { get; set; } = true;

        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }
}
