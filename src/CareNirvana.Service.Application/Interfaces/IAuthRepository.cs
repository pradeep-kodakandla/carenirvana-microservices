using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthDetailRow?> GetAuthByNumberAsync(string authNumber, bool includeDeleted = false);
        Task<AuthDetailRow?> GetAuthByIdAsync(long authDetailId, bool includeDeleted = false);
        Task<List<AuthDetailRow>> GetAuthsByMemberAsync(int memberDetailsId, bool includeDeleted = false);

        Task<long> CreateAuthAsync(CreateAuthRequest req, int userId);
        Task UpdateAuthAsync(long authDetailId, UpdateAuthRequest req, int userId);
        Task SoftDeleteAuthAsync(long authDetailId, int userId);

        Task<TemplateSectionsResponse?> GetDecisionTemplateAsync(int authTemplateId, CancellationToken ct = default);

        Task<IReadOnlyList<DecisionSectionItemDto>> GetDecisionSectionItemsAsync(long authDetailId, string sectionName, CancellationToken ct = default);
        Task<Guid> InsertDecisionSectionItemAsync(long authDetailId, string sectionName, CreateDecisionSectionItemRequest req, int userId, CancellationToken ct = default);
        Task<bool> UpdateDecisionSectionItemAsync(long authDetailId, string sectionName, Guid itemId, UpdateDecisionSectionItemRequest req, int userId, CancellationToken ct = default);
        Task<bool> SoftDeleteDecisionSectionItemAsync(long authDetailId, string sectionName, Guid itemId, int userId, CancellationToken ct = default);

        Task AcceptRejectAuthWorkgroupAsync( long authWorkgroupId, string actionType, string? comment, int userId, int completedStatusId   );
    }

    public interface IAuthNotesRepository
    {
        Task<TemplateSectionResponse?> GetAuthNotesTemplateAsync(int authTemplateId, CancellationToken ct = default);
        Task<IReadOnlyList<AuthNoteDto>> GetNotesAsync(long authDetailId, CancellationToken ct = default);
        Task<Guid> InsertNoteAsync(long authDetailId, CreateAuthNoteRequest req, int userId, CancellationToken ct = default);
        Task<bool> UpdateNoteAsync(long authDetailId, Guid noteId, UpdateAuthNoteRequest req, int userId, CancellationToken ct = default);
        Task<bool> SoftDeleteNoteAsync(long authDetailId, Guid noteId, int userId, CancellationToken ct = default);
    }

    public interface IAuthDocumentsRepository
    {
        Task<TemplateSectionResponse?> GetAuthDocumentsTemplateAsync(int authTemplateId, CancellationToken ct = default);
        Task<IReadOnlyList<AuthDocumentDto>> GetDocumentsAsync(long authDetailId, CancellationToken ct = default);
        Task<Guid> InsertDocumentAsync(long authDetailId, CreateAuthDocumentRequest req, int userId, CancellationToken ct = default);
        Task<bool> UpdateDocumentAsync(long authDetailId, Guid documentId, UpdateAuthDocumentRequest req, int userId, CancellationToken ct = default);
        Task<bool> SoftDeleteDocumentAsync(long authDetailId, Guid documentId, int userId, CancellationToken ct = default);
    }


}
