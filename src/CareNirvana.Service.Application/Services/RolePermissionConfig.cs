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

        public Task<IEnumerable<CfgRole>> GetAllAsync() => _repo.GetAllAsync();
        public Task<CfgRole?> GetByIdAsync(int roleId) => _repo.GetByIdAsync(roleId);
        public Task<int> AddAsync(CfgRole role) => _repo.AddAsync(role);
        public Task UpdateAsync(CfgRole role) => _repo.UpdateAsync(role);
        public Task DeleteAsync(int roleId, int deletedBy) => _repo.DeleteAsync(roleId, deletedBy);

    }

}
