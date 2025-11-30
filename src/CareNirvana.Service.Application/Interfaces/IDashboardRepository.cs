using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<MemberCareStaff>> GetMyCareStaff(int userId);
        Task<DashboardCounts> DashBoardCount(int userId);
        Task<List<MemberSummary>> GetMemberSummaries(int userId);
        Task<List<MemberSummary>> GetMemberSummary(int memberdetailsid);
        Task<List<AuthDetailListItem>> GetAuthDetailListAsync(int userId);
        Task<List<ActivityItem>> GetPendingActivitiesAsync(int? userId = null);
        Task<List<ActivityItem>> GetPendingWQAsync(int? userId = null);
        Task<List<AuthActivityLine>> GetWQActivityLines(int? activityid = null);
        Task<int> UpdateAuthActivityLinesAsync(IEnumerable<int> lineIds, string status, string mdDecision, string mdNotes, int reviewedByUserId);
        Task<long> InsertFaxFileAsync(FaxFile fax);
        Task<int> UpdateFaxFileAsync(FaxFile fax);
        Task<(List<FaxFile> Items, int Total)> GetFaxFilesAsync(string? search, int page, int pageSize, string? status);
        Task<FaxFile?> GetFaxFileByIdAsync(long faxId);
        Task<int> EndMemberCareStaffAsync(int memberDetailsId, DateTime endDate, int? careStaffId = null, int? updatedBy = null, CancellationToken ct = default);
        Task<List<UserWorkGroupWorkBasketItem>> GetUserWorkGroupWorkBasketsAsync(int userId);
        Task<List<ActivityRequestItem>> GetRequestActivitiesAsync(int? userId = null);

        Task<IEnumerable<MemberSearchResultDto>> SearchMembersAsync(MemberSearchCriteriaDto criteria);
    }
}
