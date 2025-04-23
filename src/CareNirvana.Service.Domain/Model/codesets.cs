using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class codesets
    {
        public int? Id { get; set; }
        public string code { get; set; }
        public string codeDesc { get; set; }
        public string codeShortDesc { get; set; }
        public DateTime? effectiveDate { get; set; }
        public DateTime? endDate { get; set; }
        public string severity { get; set; }
        public string laterality { get; set; }
        public string activeFlag { get; set; }
        public string type { get; set; }
    }
}