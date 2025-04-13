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

        public async Task<List<AuthTemplate>> ExecuteAsync(int authclassId)
        {
            return await _repository.GetAllAsync(authclassId);
        }

        public async Task<List<AuthTemplate>> GetTemplate(int id)
        {
            return await _repository.GetAuthTemplate(id);
        }
        public async Task ExecuteAsync(AuthTemplate authTemplate)
        {
            var AuthTemplate = new AuthTemplate
            {
                JsonContent = authTemplate.JsonContent,
                CreatedOn = System.DateTime.UtcNow,
                TemplateName = authTemplate.TemplateName,
                Id = authTemplate.Id,
                CreatedBy = authTemplate.CreatedBy,
            };

            await _repository.SaveAsync(authTemplate);
        }
    }
}
