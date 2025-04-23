using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.UseCases
{
    public class CodesetsService : ICodesetsRepository
    {
        private readonly ICodesetsRepository _repository;

        public CodesetsService(ICodesetsRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<codesets>> GetAllAsync(string type)
        {
            return await _repository.GetAllAsync(type);
        }

        public async Task<codesets?> GetByIdAsync(string id, string type)
        {
            return await _repository.GetByIdAsync(id, type);
        }

        public async Task<codesets> InsertAsync(codesets entity)
        {
            return await _repository.InsertAsync(entity);
        }

        public async Task<codesets> UpdateAsync(codesets entity)
        {
            return await _repository.UpdateAsync(entity);
        }
    }
}
