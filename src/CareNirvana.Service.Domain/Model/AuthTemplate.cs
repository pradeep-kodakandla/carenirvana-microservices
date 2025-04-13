using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class AuthTemplate
    {
        public int? Id { get; set; }
        public string TemplateName { get; set; }
        public string JsonContent { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public int? authclassid { get; set; }
    }
}
