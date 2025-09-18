using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberCareStaff
    {
        public int MemberCareStaffId { get; set; }   // Primary Key
        public int? UserId { get; set; }
        public int? MemberDetailsId { get; set; }
        public bool ActiveFlag { get; set; } = true;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }
}
