using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Infrastructure.Repository;
using System.Reflection;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.API.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionController : Controller
    {
        private readonly IRolePermissionConfigRepository _configRepository;
        public RolePermissionController(IRolePermissionConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }
        [HttpGet("modules")]
        public async Task<ActionResult<IEnumerable<CfgModule>>> GetModules()
        {
            var modules = await _configRepository.GetModulesAsync();
            return Ok(modules);
        }

        [HttpGet("featuregroups/{moduleId}")]
        public async Task<ActionResult<IEnumerable<CfgFeatureGroup>>> GetFeatureGroupsByModule(int moduleId)
        {
            var groups = await _configRepository.GetFeatureGroupsByModuleAsync(moduleId);
            return Ok(groups);
        }

        [HttpGet("features/{featureGroupId}")]
        public async Task<ActionResult<IEnumerable<CfgFeature>>> GetFeaturesByFeatureGroup(int featureGroupId)
        {
            var features = await _configRepository.GetFeaturesByFeatureGroupAsync(featureGroupId);
            return Ok(features);
        }

        [HttpGet("resources/{featureId}")]
        public async Task<ActionResult<IEnumerable<CfgResource>>> GetResourcesByFeature(int featureId)
        {
            var resources = await _configRepository.GetResourcesByFeatureAsync(featureId);
            return Ok(resources);
        }
    }
}
