using System;

namespace CareNirvana.Service.Domain.Model
{
    public class Cfgvalidation
    {
        public int? validationId { get; set; }
        public string validationJson { get; set; }  // jsonb stored as string
        public string validationName { get; set; }
        public int moduleId { get; set; }

        public bool activeFlag { get; set; } = true;

        public DateTime? createdOn { get; set; }
        public int? createdBy { get; set; }

        public DateTime? updatedOn { get; set; }
        public int? updatedBy { get; set; }

        public DateTime? deletedOn { get; set; }
        public int? deletedBy { get; set; }
    }
}
