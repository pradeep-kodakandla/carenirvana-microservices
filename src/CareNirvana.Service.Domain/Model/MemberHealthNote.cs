using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberHealthNote
    {
        public long MemberHealthNotesId { get; set; }
        public long MemberId { get; set; }
        public int? NoteTypeId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool IsAlert { get; set; }

        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }

        // Convenience
        public bool IsDeleted => DeletedOn.HasValue;
    }
}
