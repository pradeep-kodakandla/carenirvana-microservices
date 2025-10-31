using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class WorkGroup
    {
        public int WorkGroupId { get; set; }                // workgroupid
        public string WorkGroupCode { get; set; } = "";     // workgroupcode
        public string WorkGroupName { get; set; } = "";     // workgroupname
        public string? Description { get; set; }            // description
        public bool IsFax { get; set; }                     // isfax
        public bool IsProviderPortal { get; set; }          // isproviderportal
        public bool ActiveFlag { get; set; } = true;        // activeflag

        // Audit
        public string CreatedBy { get; set; } = "";         // createdby
        public DateTimeOffset CreatedOn { get; set; }       // createdon (timestamptz)
        public string? UpdatedBy { get; set; }              // updatedby
        public DateTimeOffset? UpdatedOn { get; set; }      // updatedon
        public string? DeletedBy { get; set; }              // deletedby
        public DateTimeOffset? DeletedOn { get; set; }      // deletedon
    }

    public class WorkGroupCreateDto
    {
        public string WorkGroupCode { get; set; } = "";
        public string WorkGroupName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsFax { get; set; }
        public bool IsProviderPortal { get; set; }
        public string CreatedBy { get; set; } = "";
    }

    public class WorkGroupUpdateDto
    {
        public int WorkGroupId { get; set; }
        public string WorkGroupCode { get; set; } = "";
        public string WorkGroupName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsFax { get; set; }
        public bool IsProviderPortal { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public string UpdatedBy { get; set; } = "";
    }
}
