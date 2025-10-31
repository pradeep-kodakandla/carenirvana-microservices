using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IWorkGroup
    {
        Task<int> CreateAsync(WorkGroup entity);
        Task<WorkGroup?> GetByIdAsync(int workGroupId);
        Task<IEnumerable<WorkGroup>> GetAllAsync(bool includeInactive = false);
        Task<bool> ExistsByNameAsync(string workGroupName, int? excludeId = null);
        Task<bool> ExistsByCodeAsync(string workGroupCode, int? excludeId = null);
        Task<int> UpdateAsync(WorkGroup entity);
        Task<int> SoftDeleteAsync(int workGroupId, string deletedBy);
        Task<int> RestoreAsync(int workGroupId, string updatedBy);
        Task<int> HardDeleteAsync(int workGroupId);
    }
}
