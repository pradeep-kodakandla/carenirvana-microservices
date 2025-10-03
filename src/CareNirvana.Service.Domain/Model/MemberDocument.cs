using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberDocument
    {
        public long MemberDocumentId { get; set; }
        public long MemberId { get; set; }
        public int? DocumentTypeId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public byte[] DocumentBytes { get; set; } = Array.Empty<byte>();

        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }

        public bool IsDeleted => DeletedOn.HasValue;
    }
}
