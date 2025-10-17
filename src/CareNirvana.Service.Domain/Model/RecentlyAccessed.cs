using System;

namespace CareNirvana.Service.Domain.Model
{
    public class RecentlyAccessed
    {
        public int RecentlyAccessedId { get; set; }   // filled by DB on insert
        public int UserId { get; set; }
        public int? FeatureId { get; set; }
        public int? FeatureGroupId { get; set; }
        public DateTime? AccessedDateTime { get; set; } // optional on insert; DB default used if null
        public string? Action { get; set; }
        public int MemberDetailsId { get; set; }
        public int? AuthDetailId { get; set; }
        public int? ComplaintDetailId { get; set; }
    }

    public class RecentlyAccessedView
    {
        public int RecentlyAccessedId { get; set; }
        public int UserId { get; set; }
        public int? FeatureId { get; set; }
        public string? FeatureName { get; set; }           // from cfgfeature
        public int? FeatureGroupId { get; set; }
        public string? FeatureGroupName { get; set; }      // from cfgfeaturegroup
        public DateTime AccessedDateTime { get; set; }
        public string? Action { get; set; }
        public int MemberDetailsId { get; set; }
        public int? AuthDetailId { get; set; }
        public int? ComplaintDetailId { get; set; }
        public string? MemberID { get; set; }              
        public string? AuthNumber { get; set; }    
        public string? MemberName { get; set; }
    }

    public class Last24hCounts
    {
        public int MemberAccessCount { get; set; }        // rows in last 24h (memberdetailsid always present)
        public int AuthorizationAccessCount { get; set; } // rows with authdetailid not null in last 24h
        public int ComplaintAccessCount { get; set; }     // rows with complaintdetailid not null in last 24h
    }

}
