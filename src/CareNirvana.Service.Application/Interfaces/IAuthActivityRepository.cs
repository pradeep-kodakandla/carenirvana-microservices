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
    }
}
