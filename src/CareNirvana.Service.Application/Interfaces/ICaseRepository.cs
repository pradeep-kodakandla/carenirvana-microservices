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
    }


}
