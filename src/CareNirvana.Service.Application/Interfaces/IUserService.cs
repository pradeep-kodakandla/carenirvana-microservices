using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IUserService
    {
        SecurityUser Authenticate(string username, string password);
        Task<List<SecurityUser>> GetUserDetails();


        Task<IEnumerable<SecurityUser>> GetAllAsync();
        Task<SecurityUser?> GetByIdAsync(int userId);
        Task<int> AddAsync(SecurityUser user);
        Task UpdateAsync(SecurityUser user);
        Task DeleteAsync(int userId, int deletedBy);
    }
}
