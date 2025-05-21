using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class CfgResourceField
    {
        public int ResourceFieldId { get; set; }
        public int ResourceId { get; set; }
        public string FieldName { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowVisible { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }

}
