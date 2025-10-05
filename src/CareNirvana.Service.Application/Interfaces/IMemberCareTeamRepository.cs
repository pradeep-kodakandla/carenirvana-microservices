using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberCareTeamRepository
    {
        Task<MemberCareStaffView?> GetAsync(int memberCareStaffId);
        Task<PagedResult<MemberCareStaffView>> ListAsync(
            int? userId, int? memberDetailsId, bool includeInactive,
            int page, int pageSize, string? search = null);

        Task<int> CreateAsync(MemberCareStaffCreateRequest req);
        Task<bool> UpdateAsync(int memberCareStaffId, MemberCareStaffUpdateRequest req);
        Task<bool> SoftDeleteAsync(int memberCareStaffId, int? deletedBy);
    }
}
