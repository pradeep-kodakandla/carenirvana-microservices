using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MemberEnrollment
    {
        public int MemberEnrollmentId { get; set; }
        public int MemberDetailsId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Boolean Status { get; set; }
        public string HierarchyPath { get; set; }
        public string LevelMap { get; set; }
        public string Levels { get; set; }
    }
}
