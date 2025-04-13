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

        public Task<IEnumerable<AuthActivity>> GetAllAsync() => _repo.GetAllAsync();
        public Task<AuthActivity?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<AuthActivity> InsertAsync(AuthActivity activity) => _repo.InsertAsync(activity);
        public Task<AuthActivity> UpdateAsync(AuthActivity activity) => _repo.UpdateAsync(activity);
    }
}
