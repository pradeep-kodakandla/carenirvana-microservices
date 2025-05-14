using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class CfgRole
    {
        public int RoleId { get; set; }
        public string Name { get; set; }
        public string ManagerAccess { get; set; }
        public string QocAccess { get; set; }
        public string Sensitive { get; set; }
        public string Permissions { get; set; } // Stored as JSON string
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

}
