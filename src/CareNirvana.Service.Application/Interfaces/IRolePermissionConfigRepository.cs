using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IRolePermissionConfigRepository
    {
        Task<IEnumerable<CfgModule>> GetModulesAsync();
        Task<IEnumerable<CfgFeatureGroup>> GetFeatureGroupsByModuleAsync(int moduleId);
        Task<IEnumerable<CfgFeature>> GetFeaturesByFeatureGroupAsync(int featureGroupId);
        Task<IEnumerable<CfgResource>> GetResourcesByFeatureAsync(int featureId);
    }
}
