using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class WorkBasket
    {
        public int WorkBasketId { get; set; }                   // workbasketid
        public string WorkBasketCode { get; set; } = "";        // workbasketcode
        public string WorkBasketName { get; set; } = "";        // workbasketname
        public string? Description { get; set; }                // description
        public bool ActiveFlag { get; set; } = true;            // activeflag

        public string CreatedBy { get; set; } = "";             // createdby
        public DateTimeOffset CreatedOn { get; set; }           // createdon
        public string? UpdatedBy { get; set; }                  // updatedby
        public DateTimeOffset? UpdatedOn { get; set; }          // updatedon
        public string? DeletedBy { get; set; }                  // deletedby
        public DateTimeOffset? DeletedOn { get; set; }          // deletedon
    }

    public class WorkGroupWorkBasket
    {
        public int WorkGroupWorkBasketId { get; set; }          // workgroupworkbasketid
        public int WorkGroupId { get; set; }                    // workgroupid
        public int WorkBasketId { get; set; }                   // workbasketid
        public bool ActiveFlag { get; set; } = true;            // activeflag

        public string CreatedBy { get; set; } = "";             // createdby
        public DateTimeOffset CreatedOn { get; set; }           // createdon
        public string? UpdatedBy { get; set; }                  // updatedby
        public DateTimeOffset? UpdatedOn { get; set; }          // updatedon
        public string? DeletedBy { get; set; }                  // deletedby
        public DateTimeOffset? DeletedOn { get; set; }          // deletedon
    }

    public class WorkBasketCreateDto
    {
        public string WorkBasketCode { get; set; } = "";
        public string WorkBasketName { get; set; } = "";
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = "";
        public List<int> WorkGroupIds { get; set; } = new();
    }

    public class WorkBasketUpdateDto
    {
        public int WorkBasketId { get; set; }
        public string WorkBasketCode { get; set; } = "";
        public string WorkBasketName { get; set; } = "";
        public string? Description { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public string UpdatedBy { get; set; } = "";
        public List<int> WorkGroupIds { get; set; } = new();
    }

    // API view model
    public class WorkBasketView
    {
        public int WorkBasketId { get; set; }
        public string WorkBasketCode { get; set; } = "";
        public string WorkBasketName { get; set; } = "";
        public string? Description { get; set; }
        public bool ActiveFlag { get; set; }
        public List<int> WorkGroupIds { get; set; } = new();
    }

    public class UserWorkGroupAssignment
    {
        public int UserWorkGroupId { get; set; }             // cfguserworkgroup.userworkgroupid
        public int UserId { get; set; }                      // securityuser.userid
        public string? UserFullName { get; set; }            // First + Last (fallback to username/email)
        public int WorkGroupWorkBasketId { get; set; }       // cfgworkgroupworkbasket.workgroupworkbasketid

        public int WorkGroupId { get; set; }                 // cfgworkgroup.workgroupid
        public string? WorkGroupCode { get; set; }           // optional if you have it
        public string? WorkGroupName { get; set; }

        public int WorkBasketId { get; set; }                // cfgworkbasket.workbasketid
        public string? WorkBasketCode { get; set; }          // optional if you have it
        public string? WorkBasketName { get; set; }

        public bool ActiveFlag { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTimeOffset? CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset? DeletedOn { get; set; }
    }


}
