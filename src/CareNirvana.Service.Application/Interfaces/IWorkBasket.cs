using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IWorkBasket
    {
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);

        Task<int> CreateWithGroupsAsync(WorkBasket basket, IEnumerable<int> workGroupIds);
        Task<int> UpdateWithGroupsAsync(WorkBasket basket, IEnumerable<int> workGroupIds);

        Task<WorkBasket?> GetByIdAsync(int id);
        Task<IEnumerable<WorkBasket>> GetAllAsync(bool includeInactive = false);

        Task<List<int>> GetLinkedWorkGroupIdsAsync(int workBasketId);

        Task<int> SoftDeleteAsync(int id, string deletedBy);
        Task<int> HardDeleteAsync(int id);

        Task<IEnumerable<UserWorkGroupAssignment>> GetUserWorkGroupsAsync(int userId);
    }
}
