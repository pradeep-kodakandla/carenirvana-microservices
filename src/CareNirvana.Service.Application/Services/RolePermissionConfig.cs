using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Services
{
    class RolePermissionConfig : IRolePermissionConfigRepository
    {
        private readonly IRolePermissionConfigRepository _repo;

        public RolePermissionConfig(IRolePermissionConfigRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<CfgModule>> GetModulesAsync() => _repo.GetModulesAsync();

        public Task<IEnumerable<CfgFeatureGroup>> GetFeatureGroupsByModuleAsync(int moduleId) => _repo.GetFeatureGroupsByModuleAsync(moduleId);

        public Task<IEnumerable<CfgFeature>> GetFeaturesByFeatureGroupAsync(int featureGroupId) => _repo.GetFeaturesByFeatureGroupAsync(featureGroupId);

        public Task<IEnumerable<CfgResource>> GetResourcesByFeatureAsync(int featureId) => _repo.GetResourcesByFeatureAsync(featureId);

    }

}
