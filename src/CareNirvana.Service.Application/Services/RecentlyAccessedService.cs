using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Services
{
    internal class RecentlyAccessedService : IRecentlyAccessed
    {
        private readonly IRecentlyAccessed _repo;
        public RecentlyAccessedService(IRecentlyAccessed repo)
        {
            _repo = repo;
        }
        public Task<int> InsertAsync(RecentlyAccessed item) => _repo.InsertAsync(item);
        public Task<IEnumerable<RecentlyAccessedView>> GetByUserAsync(int userId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100, int offset = 0) => _repo.GetByUserAsync(userId, fromUtc, toUtc, limit, offset);
        public Task<Last24hCounts> GetLast24hCountsAsync(int userId) => _repo.GetLast24hCountsAsync(userId);

    }
}
