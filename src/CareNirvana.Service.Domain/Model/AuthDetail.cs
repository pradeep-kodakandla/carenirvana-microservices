using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class AuthDetail
    {
        public int? Id { get; set; }
        public List<object>? Data { get; set; } // json object
        public DateTime? CreatedOn { get; set; }
        public string? AuthNumber { get; set; }
        public int? AuthTypeId { get; set; }
        public int? MemberId { get; set; }
        public DateTime? AuthDueDate { get; set; }
        public DateTime? NextReviewDate { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string SaveType { get; set; }
        public string TreatmentType { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }
        public string responseData { get; set; } // Store as a JSON string
    }
}
