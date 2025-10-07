using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services
{
    public class DashboardService : IDashboardRepository
    {
        private readonly IDashboardRepository _repo;
        public DashboardService(IDashboardRepository repo)
        {
            _repo = repo;
        }
        public Task<List<MemberCareStaff>> GetMyCareStaff(int userId) => _repo.GetMyCareStaff(userId);
        public Task<DashboardCounts> DashBoardCount(int userId) => _repo.DashBoardCount(userId);
        public Task<List<MemberSummary>> GetMemberSummaries(int userId) => _repo.GetMemberSummaries(userId);
        public Task<List<MemberSummary>> GetMemberSummary(int memberdetailsid) => _repo.GetMemberSummary(memberdetailsid);
        public Task<List<AuthDetailListItem>> GetAuthDetailListAsync(int userId) => _repo.GetAuthDetailListAsync(userId);
        public Task<List<AuthActivityItem>> GetPendingAuthActivitiesAsync(int? userId = null) => _repo.GetPendingAuthActivitiesAsync(userId);
        public Task<List<AuthActivityItem>> GetPendingWQAsync(int? userId = null) => _repo.GetPendingWQAsync(userId);
        public Task<List<AuthActivityLine>> GetWQActivityLines(int? activityid = null) => _repo.GetWQActivityLines(activityid);
        public Task<int> UpdateAuthActivityLinesAsync(IEnumerable<int> lineIds, string status, string mdDecision, string mdNotes, int reviewedByUserId) => _repo.UpdateAuthActivityLinesAsync(lineIds, status, mdDecision, mdNotes, reviewedByUserId);
        public Task<long> InsertFaxFileAsync(FaxFile fax) => _repo.InsertFaxFileAsync(fax);
        public Task<int> UpdateFaxFileAsync(FaxFile fax) => _repo.UpdateFaxFileAsync(fax);
        public Task<(List<FaxFile> Items, int Total)> GetFaxFilesAsync(string? search, int page, int pageSize, string? status) => _repo.GetFaxFilesAsync(search, page, pageSize, status);
        public Task<FaxFile?> GetFaxFileByIdAsync(long faxId) => _repo.GetFaxFileByIdAsync(faxId);  
    }

}
