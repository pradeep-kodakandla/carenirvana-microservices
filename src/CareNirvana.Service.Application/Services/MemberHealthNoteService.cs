using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services

{
    internal class MemberHealthNoteService : IMemberNotes
    {
        private readonly IMemberNotes _repo;
        public MemberHealthNoteService(IMemberNotes repo)
        {
            _repo = repo;
        }

        public Task<long> InsertMemberHealthNoteAsync(MemberHealthNote note) => _repo.InsertMemberHealthNoteAsync(note);
        public Task<int> UpdateMemberHealthNoteAsync(MemberHealthNote note) => _repo.UpdateMemberHealthNoteAsync(note);
        public Task<int> SoftDeleteMemberHealthNoteAsync(long memberHealthNotesId, int deletedBy) => _repo.SoftDeleteMemberHealthNoteAsync(memberHealthNotesId, deletedBy);
        public Task<MemberHealthNote?> GetMemberHealthNoteByIdAsync(long id) => _repo.GetMemberHealthNoteByIdAsync(id);
        public Task<(List<MemberHealthNote> Items, int Total)> GetMemberHealthNotesForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false) => _repo.GetMemberHealthNotesForMemberAsync(memberId, page, pageSize, includeDeleted);
        public Task<List<MemberHealthNote>> GetActiveAlertsForMemberAsync(long memberId) => _repo.GetActiveAlertsForMemberAsync(memberId);

    }
}
