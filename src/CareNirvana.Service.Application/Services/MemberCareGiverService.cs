using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services

{
    internal class MemberCareGiverService : IMemberCareGiverRepository
    {
        private readonly IMemberCareGiverRepository _repo;
        public MemberCareGiverService(IMemberCareGiverRepository repo)
        {
            _repo = repo;
        }

        public Task<IReadOnlyList<MemberCaregiverDto>> GetBundleByMemberDetailsIdAsync(int memberDetailsId) => _repo.GetBundleByMemberDetailsIdAsync(memberDetailsId);

        // CRUD for membercaregiver
        public Task<int> CreateAsync(MemberCaregiver model) => _repo.CreateAsync(model);
        public Task<MemberCaregiver?> GetByIdAsync(int memberCaregiverId) => _repo.GetByIdAsync(memberCaregiverId);
        public Task<bool> UpdateAsync(MemberCaregiver model) => _repo.UpdateAsync(model);
        public Task<bool> SoftDeleteAsync(int memberCaregiverId, int deletedBy) => _repo.SoftDeleteAsync(memberCaregiverId, deletedBy);
    }
}
