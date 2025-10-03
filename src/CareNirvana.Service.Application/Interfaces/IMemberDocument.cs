using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberDocument
    {
        Task<long> InsertMemberDocumentAsync(MemberDocument doc);
        Task<int> UpdateMemberDocumentAsync(MemberDocument doc);
        Task<int> SoftDeleteMemberDocumentAsync(long memberDocumentId, int deletedBy);

        Task<MemberDocument?> GetMemberDocumentByIdAsync(long id);
        Task<(List<MemberDocument> Items, int Total)> GetMemberDocumentsForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false);
    }
}
