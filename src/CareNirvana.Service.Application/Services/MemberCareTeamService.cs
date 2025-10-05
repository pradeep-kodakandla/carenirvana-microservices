using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services

{
    internal class MemberCareTeamService : IMemberCareTeamRepository
    {
        private readonly IMemberCareTeamRepository _repo;
        public MemberCareTeamService(IMemberCareTeamRepository repo)
        {
            _repo = repo;
        }
        public Task<int> CreateAsync(MemberCareStaffCreateRequest req) => _repo.CreateAsync(req);
        public Task<MemberCareStaffView?> GetAsync(int memberCareStaffId) => _repo.GetAsync(memberCareStaffId);
        public Task<PagedResult<MemberCareStaffView>> ListAsync(int? userId, int? memberDetailsId, bool includeInactive, int page, int pageSize, string? search = null) => _repo.ListAsync(userId, memberDetailsId, includeInactive, page, pageSize, search);
        public Task<bool> SoftDeleteAsync(int memberCareStaffId, int? deletedBy) => _repo.SoftDeleteAsync(memberCareStaffId, deletedBy);
        public Task<bool> UpdateAsync(int memberCareStaffId, MemberCareStaffUpdateRequest req) => _repo.UpdateAsync(memberCareStaffId, req);

    }
}
