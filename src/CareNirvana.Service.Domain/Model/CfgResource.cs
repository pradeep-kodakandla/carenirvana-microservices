using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class CfgResource
    {
        public int FeatureResourceId { get; set; }
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public bool AllowView { get; set; }
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowPrint { get; set; }
        public bool AllowDownload { get; set; }
    }
}
