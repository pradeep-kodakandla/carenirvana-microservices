using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IRecentlyAccessed
    {
        Task<int> InsertAsync(RecentlyAccessed item);
        Task<IEnumerable<RecentlyAccessedView>> GetByUserAsync(int userId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100, int offset = 0);
        Task<Last24hCounts> GetLast24hCountsAsync(int userId);
    }
}
