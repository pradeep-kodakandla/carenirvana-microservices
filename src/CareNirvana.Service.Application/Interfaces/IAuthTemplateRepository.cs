using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IAuthTemplateRepository
    {
        Task<List<AuthTemplate>> GetAllAsync();
        Task<List<AuthTemplate>> GetAuthTemplate(int id);
        Task SaveAsync(AuthTemplate authTemplate);
    }
}
