using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface ICaseActivityRepository
    {
        Task<IReadOnlyList<CaseActivityRowDto>> GetByCaseAsync(int caseHeaderId, int memberDetailsId, int caseLevelId, string status, CancellationToken ct);
        Task<CaseActivityRowDto?> GetByIdAsync(int caseActivityId, CancellationToken ct);

        Task<int> InsertAsync(CaseActivityCreateDto dto, CancellationToken ct);
        Task<bool> UpdateAsync(CaseActivityUpdateDto dto, CancellationToken ct);
        Task<bool> DeleteAsync(int caseActivityId, int deletedBy, CancellationToken ct);

        Task<bool> AcceptGroupActivityAsync(int caseActivityId, WorkgroupActionDto dto, CancellationToken ct);
        Task<bool> RejectGroupActivityAsync(int caseActivityId, WorkgroupActionDto dto, CancellationToken ct);

        // “More GETs”
        Task<IReadOnlyList<CaseActivityRowDto>> GetPendingRequestsForUserAsync(int userId, int caseHeaderId, int memberDetailsId, int caseLevelId, CancellationToken ct);
        Task<IReadOnlyList<CaseActivityRowDto>> GetAcceptedForUserAsync(int userId, int caseHeaderId, int memberDetailsId, int caseLevelId, CancellationToken ct);

        Task<CaseActivityTemplateResponse?> GetCaseActivityTemplateAsync(int caseTemplateId, CancellationToken ct = default);
    }

}
