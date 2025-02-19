using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    internal class CfgAdminData
    {
        public string Module { get; set; } = string.Empty;
        public JsonElement JsonContent { get; set; }
    }
}
