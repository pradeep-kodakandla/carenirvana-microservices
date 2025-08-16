using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IAuthActivityRepository
    {
        Task<IEnumerable<AuthActivity>> GetAllAsync(int authdetailid);
        Task<AuthActivity?> GetByIdAsync(int id);
        Task<AuthActivity> InsertAsync(AuthActivity activity);
        Task<AuthActivity> UpdateAsync(AuthActivity activity);


        Task<List<(AuthActivity Activity, List<dynamic> Lines)>> GetMdReviewActivitiesAsync( int? activityId = null, int? authDetailId = null);

        Task<int> CreateMdReviewActivityAsync(MdReviewActivityCreate payload);

        // Update a single line (decision/status/notes) with optional optimistic concurrency
        Task<bool> UpdateMdReviewLineAsync(
            long activityId,
            long lineId,
            string mdDecision,
            string status,
            string? mdNotes,
            int? reviewedByUserId,
            long? expectedVersion = null);
    }

}
