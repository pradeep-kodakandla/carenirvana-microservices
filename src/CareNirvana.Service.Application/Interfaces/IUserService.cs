using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IUserService
    {
        SecurityUser Authenticate(string username, string password);
    }
}
