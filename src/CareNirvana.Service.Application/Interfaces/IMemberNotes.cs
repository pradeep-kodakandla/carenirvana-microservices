using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberNotes
    {
        Task<long> InsertMemberHealthNoteAsync(MemberHealthNote note);
        Task<int> UpdateMemberHealthNoteAsync(MemberHealthNote note);
        Task<int> SoftDeleteMemberHealthNoteAsync(long memberHealthNotesId, int deletedBy);

        Task<MemberHealthNote?> GetMemberHealthNoteByIdAsync(long id);

        Task<(List<MemberHealthNote> Items, int Total)>
            GetMemberHealthNotesForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false);

        Task<List<MemberHealthNote>> GetActiveAlertsForMemberAsync(long memberId);
    }
}
