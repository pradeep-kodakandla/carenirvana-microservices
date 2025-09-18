using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class DashboardCounts
    {
        public int MyMemberCount { get; set; }
        public int AuthCount { get; set; }
        public int RequestCount { get; set; }
        public int ComplaintCount { get; set; }
        public int FaxCount { get; set; }
        public int WQCount { get; set; }
        public int ActivityCount { get; set; }
    }
}
