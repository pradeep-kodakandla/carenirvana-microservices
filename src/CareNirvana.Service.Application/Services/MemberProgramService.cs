using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services

{
    internal class MemberProgramService : IMemberProgramRepository
    {
        private readonly IMemberProgramRepository _repo;
        public MemberProgramService(IMemberProgramRepository repo)
        {
            _repo = repo;
        }
        public Task<int> InsertMemberProgramAsync(MemberProgram mp) => _repo.InsertMemberProgramAsync(mp);
        public Task<int> UpdateMemberProgramAsync(MemberProgram mp) => _repo.UpdateMemberProgramAsync(mp);
        public Task<int> SoftDeleteMemberProgramAsync(int memberProgramId, int deletedBy) => _repo.SoftDeleteMemberProgramAsync(memberProgramId, deletedBy);
        public Task<MemberProgram?> GetMemberProgramByIdAsync(int id) => _repo.GetMemberProgramByIdAsync(id);
        public Task<(List<MemberProgram> Items, int Total)> GetMemberProgramsForMemberAsync(int memberDetailsId, int page = 1, int pageSize = 25, bool includeDeleted = false) => _repo.GetMemberProgramsForMemberAsync(memberDetailsId, page, pageSize, includeDeleted);
        public Task<List<MemberProgram>> GetActiveMemberProgramsForMemberAsync(int memberDetailsId) => _repo.GetActiveMemberProgramsForMemberAsync(memberDetailsId);
    }
}
