using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services
{
    public class MemberAlertService : IMemberAlertRepository
    {
        private readonly IMemberAlertRepository _repo;
        public MemberAlertService(IMemberAlertRepository repo)
        {
            _repo = repo;
        }
        public Task<MemberAlertPagedResult> GetAlertsAsync(int[]? memberDetailsIds = null, int? alertId = null, bool activeOnly = true, int page = 1, int pageSize = 50)
        {
            return _repo.GetAlertsAsync(memberDetailsIds, alertId, activeOnly, page, pageSize);
        }
        public Task<int?> UpdateAlertStatusAsync(int memberAlertId, int? alertStatusId = null, DateTime? dismissedDate = null, DateTime? acknowledgedDate = null, int updatedBy = 0)
        {
            return _repo.UpdateAlertStatusAsync(memberAlertId, alertStatusId, dismissedDate, acknowledgedDate, updatedBy);
        }
    }
}
