using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberProgramRepository
    {
        Task<int> InsertMemberProgramAsync(MemberProgram mp);
        Task<int> UpdateMemberProgramAsync(MemberProgram mp);
        Task<int> SoftDeleteMemberProgramAsync(int memberProgramId, int deletedBy);

        Task<MemberProgram?> GetMemberProgramByIdAsync(int id);

        Task<(List<MemberProgram> Items, int Total)> GetMemberProgramsForMemberAsync(
            int memberDetailsId, int page = 1, int pageSize = 25, bool includeDeleted = false);

        Task<List<MemberProgram>> GetActiveMemberProgramsForMemberAsync(int memberDetailsId);

    }
}
