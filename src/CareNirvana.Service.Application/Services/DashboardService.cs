using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Task<List<AuthDetailListItem>> GetAuthDetailListAsync(int userId) => _repo.GetAuthDetailListAsync(userId);
        public Task<List<AuthActivityItem>> GetPendingAuthActivitiesAsync(int? userId = null) => _repo.GetPendingAuthActivitiesAsync(userId);
        public Task<List<AuthActivityItem>> GetPendingWQAsync(int? userId = null) => _repo.GetPendingWQAsync(userId);
        public Task<List<AuthActivityLine>> GetWQActivityLines(int? activityid = null) => _repo.GetWQActivityLines(activityid);
        public Task<int> UpdateAuthActivityLinesAsync(IEnumerable<int> lineIds, string status, string mdDecision, string mdNotes, int reviewedByUserId) => _repo.UpdateAuthActivityLinesAsync(lineIds, status, mdDecision, mdNotes, reviewedByUserId);
    }

}
