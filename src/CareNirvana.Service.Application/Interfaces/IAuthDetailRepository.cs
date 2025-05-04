using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IAuthDetailRepository
    {
        Task<long?> SaveAsync(AuthDetail authDetail);
        Task<List<AuthDetail>> GetAllAsync(int memberId);
        Task<List<AuthDetail>> GetAuthData(string authNumber);
    }
}
