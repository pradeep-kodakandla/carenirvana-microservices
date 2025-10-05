using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberCareGiverRepository
    {
        Task<IReadOnlyList<MemberCaregiverDto>> GetBundleByMemberDetailsIdAsync(int memberDetailsId);

        // CRUD for membercaregiver
        Task<int> CreateAsync(MemberCaregiver model);
        Task<MemberCaregiver?> GetByIdAsync(int memberCaregiverId);
        Task<bool> UpdateAsync(MemberCaregiver model);
        Task<bool> SoftDeleteAsync(int memberCaregiverId, int deletedBy);
    }
}
