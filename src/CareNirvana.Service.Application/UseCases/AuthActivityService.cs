using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.UseCases
{
    public class AuthActivityService : IAuthActivityRepository
    {
        private readonly IAuthActivityRepository _repo;

        public AuthActivityService(IAuthActivityRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<AuthActivity>> GetAllAsync(int authdetailid) => _repo.GetAllAsync(authdetailid);
        public Task<AuthActivity?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<AuthActivity> InsertAsync(AuthActivity activity) => _repo.InsertAsync(activity);
        public Task<AuthActivity> UpdateAsync(AuthActivity activity) => _repo.UpdateAsync(activity);
        // -----------------------------
        // MD Review specific methods
        // -----------------------------
        public Task<List<(AuthActivity Activity, List<dynamic> Lines)>> GetMdReviewActivitiesAsync(
            int? activityId = null,
            int? authDetailId = null)
            => _repo.GetMdReviewActivitiesAsync(activityId, authDetailId);


        public Task<int> CreateMdReviewActivityAsync(MdReviewActivityCreate payload)
              => _repo.CreateMdReviewActivityAsync(payload);

        public Task<bool> UpdateMdReviewLineAsync(
            long activityId,
            long lineId,
            string mdDecision,
            string status,
            string? mdNotes,
            int? reviewedByUserId,
            long? expectedVersion = null)
            => _repo.UpdateMdReviewLineAsync(activityId, lineId, mdDecision, status, mdNotes, reviewedByUserId, expectedVersion);
    }
}