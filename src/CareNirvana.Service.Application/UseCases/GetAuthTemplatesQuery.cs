using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.UseCases
{
    public class GetAuthTemplatesQuery
    {
        private readonly IAuthTemplateRepository _repository;

        public GetAuthTemplatesQuery(IAuthTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AuthTemplate>> ExecuteAsync(int authclassId, string module)
        {
            return await _repository.GetAllAsync(authclassId, module);
        }

        public async Task<List<AuthTemplate>> GetTemplate(int id, string module)
        {
            return await _repository.GetAuthTemplate(id,module);
        }
        public async Task ExecuteAsync(AuthTemplate authTemplate, string module)
        {
            var AuthTemplate = new AuthTemplate
            {
                JsonContent = authTemplate.JsonContent,
                CreatedOn = System.DateTime.UtcNow,
                TemplateName = authTemplate.TemplateName,
                Id = authTemplate.Id,
                CreatedBy = authTemplate.CreatedBy,
            };

            await _repository.SaveAsync(authTemplate, module);
        }

        public async Task<TemplateValidation?> GetByTemplateIdAsync(int templateId)
        {
            return await _repository.GetByTemplateIdAsync(templateId);
        }
        public async Task InsertAsync(TemplateValidation entity)
        {
            await _repository.InsertAsync(entity);
        }
        public async Task UpdateAsync(TemplateValidation entity)
        {
            await _repository.UpdateAsync(entity);
        }
    }
}
