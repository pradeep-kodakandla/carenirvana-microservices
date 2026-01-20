using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface ICaseRepository
    {
        Task<CaseAggregate?> GetCaseByNumberAsync(string caseNumber, bool includeDeleted = false);
        Task<CaseAggregate?> GetCaseByHeaderIdAsync(long caseHeaderId, bool includeDeleted = false);
        Task<List<CaseAggregate>> GetCasesByMemberDetailIdAsync(long memberDetailId, bool includeDetails = false, IEnumerable<string>? statuses = null, bool includeDeleted = false);
        Task<CreateCaseResult> CreateCaseAsync(CreateCaseRequest req, long userId);
        Task<AddLevelResult> AddCaseLevelAsync(AddCaseLevelRequest req, long userId);

        Task UpdateCaseDetailAsync(UpdateCaseDetailRequest req, long userId);

        Task SoftDeleteCaseHeaderAsync(long caseHeaderId, long userId, bool cascadeDetails = true);
        Task SoftDeleteCaseDetailAsync(long caseDetailId, long userId);
        Task<IReadOnlyList<AgCaseRow>> GetAgCasesByMemberAsync(int memberDetailId, CancellationToken ct = default);

        Task AcceptRejectCaseWorkgroupAsync(
            long caseWorkgroupId,
            string actionType,      // "ACCEPT" or "REJECT"
            string? comment,
            int userId,
            int completedStatusId   // required when ACCEPT
        );
    }

    public interface ICaseNotesRepository
    {
        Task<CaseNotesTemplateResponse?> GetCaseNotesTemplateAsync(int caseTemplateId, CancellationToken ct = default);
        Task<IReadOnlyList<CaseNoteDto>> GetNotesAsync(int caseHeaderId, int levelId, CancellationToken ct = default);
        Task<Guid> InsertNoteAsync(int caseHeaderId, int levelId, CreateCaseNoteRequest req, int userId, CancellationToken ct = default);
        Task<bool> UpdateNoteAsync(int caseHeaderId, int levelId, Guid noteId, UpdateCaseNoteRequest req, int userId, CancellationToken ct = default);
        Task<bool> SoftDeleteNoteAsync(int caseHeaderId, int levelId, Guid noteId, int userId, CancellationToken ct = default);
    }

    public interface ICaseDocumentsRepository
    {
        Task<CaseDocumentsTemplateResponse?> GetCaseDocumentsTemplateAsync(int caseTemplateId, CancellationToken ct = default);

        Task<IReadOnlyList<CaseDocumentDto>> GetDocumentsAsync(int caseHeaderId, int levelId, CancellationToken ct = default);

        Task<Guid> InsertDocumentAsync(int caseHeaderId, int levelId, CreateCaseDocumentRequest req, int userId, CancellationToken ct = default);

        Task<bool> UpdateDocumentAsync(int caseHeaderId, int levelId, Guid documentId, UpdateCaseDocumentRequest req, int userId, CancellationToken ct = default);

        Task<bool> SoftDeleteDocumentAsync(int caseHeaderId, int levelId, Guid documentId, int userId, CancellationToken ct = default);
    }



}
