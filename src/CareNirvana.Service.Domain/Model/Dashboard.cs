using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    internal class Dashboard
    {
    }
    public class MemberSummary
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? MemberId { get; set; }

        public decimal? RiskScore { get; set; }
        public int? RiskLevelId { get; set; }
        public string? RiskLevelCode { get; set; }

        public DateTime? LastContact { get; set; }   // currently NULL in SQL (reserved)
        public DateTime? NextContact { get; set; }   // currently NULL in SQL (reserved)

        public string? City { get; set; }
        public int? MemberPhoneNumberId { get; set; }

        public string? LevelMap { get; set; }        // JSON string
        public int AuthCount { get; set; }           // coalesce -> non-null
        public string? DOB { get; set; }
    }
}
