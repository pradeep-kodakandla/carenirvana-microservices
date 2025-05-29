using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CareNirvana.Service.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public SecurityUser Authenticate(string username, string password)
        {
            var user = _userRepository.GetUser(username, password);
            if (user == null) return null;
            return user;
        }

        public Task<List<SecurityUser>> GetUserDetails()
        {
            return _userRepository.GetUserDetails();
        }
        public async Task<IEnumerable<SecurityUser>> GetAllAsync()
        {
            return await _userRepository.GetAllAsync();
        }
        public async Task<SecurityUser?> GetByIdAsync(int userId)
        {
            return await _userRepository.GetByIdAsync(userId);
        }
        public async Task<int> AddAsync(SecurityUser user)
        {
            return await _userRepository.AddAsync(user);
        }
        public async Task UpdateAsync(SecurityUser user)
        {
            await _userRepository.UpdateAsync(user);
        }
        public async Task DeleteAsync(int userId, int deletedBy)
        {
            await _userRepository.DeleteAsync(userId, deletedBy);
        }

    }
}

