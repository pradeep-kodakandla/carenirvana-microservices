using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services

{
    internal class MemberDocumentService : IMemberDocument
    {
        private readonly IMemberDocument _repo;
        public MemberDocumentService(IMemberDocument repo)
        {
            _repo = repo;
        }
        public Task<long> InsertMemberDocumentAsync(MemberDocument doc) => _repo.InsertMemberDocumentAsync(doc);
        public Task<int> UpdateMemberDocumentAsync(MemberDocument doc) => _repo.UpdateMemberDocumentAsync(doc);
        public Task<int> SoftDeleteMemberDocumentAsync(long memberDocumentId, int deletedBy) => _repo.SoftDeleteMemberDocumentAsync(memberDocumentId, deletedBy);
        public Task<MemberDocument?> GetMemberDocumentByIdAsync(long id) => _repo.GetMemberDocumentByIdAsync(id);
        public Task<(List<MemberDocument> Items, int Total)> GetMemberDocumentsForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false) => _repo.GetMemberDocumentsForMemberAsync(memberId, page, pageSize, includeDeleted);

    }
}