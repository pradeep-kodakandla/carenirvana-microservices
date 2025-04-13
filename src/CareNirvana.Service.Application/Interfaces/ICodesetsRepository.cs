using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface ICodesetsRepository
    {
        Task<IEnumerable<codesets>> GetAllAsync();
        Task<codesets?> GetByIdAsync(string id);
        Task<codesets> InsertAsync(codesets entity);
        Task<codesets> UpdateAsync(codesets entity);
    }
}
