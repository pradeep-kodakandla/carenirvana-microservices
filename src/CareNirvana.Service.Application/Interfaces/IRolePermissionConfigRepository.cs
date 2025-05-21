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
        Task<IEnumerable<CfgResourceField>> GetResourceFieldsByResourceIdAsync(int resourceId);

        Task<IEnumerable<CfgRole>> GetAllAsync();
        Task<CfgRole?> GetByIdAsync(int roleId);
        Task<int> AddAsync(CfgRole role);
        Task UpdateAsync(CfgRole role);
        Task DeleteAsync(int roleId, int deletedBy);
    }
}
