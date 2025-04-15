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
        Task<List<AuthTemplate>> GetAllAsync(int authClassId);
        Task<List<AuthTemplate>> GetAuthTemplate(int id);
        Task SaveAsync(AuthTemplate authTemplate);

        Task<AuthTemplateValidation?> GetByTemplateIdAsync(int templateId);
        Task InsertAsync(AuthTemplateValidation entity);
        Task UpdateAsync(AuthTemplateValidation entity);
    }
}
