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
        Task<List<AuthTemplate>> GetAllAsync(int authClassId, string module);
        Task<List<AuthTemplate>> GetAuthTemplate(int id, string module);
        Task SaveAsync(AuthTemplate authTemplate, string module);

        Task<AuthTemplateValidation?> GetByTemplateIdAsync(int templateId);
        Task InsertAsync(AuthTemplateValidation entity);
        Task UpdateAsync(AuthTemplateValidation entity);
    }
}
