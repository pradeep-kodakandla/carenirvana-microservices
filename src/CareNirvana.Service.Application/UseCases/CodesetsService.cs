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
        public async Task<IReadOnlyList<CodeSearchResult>> SearchIcdAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchIcdAsync(q, limit, ct);
        }
        public async Task<IReadOnlyList<CodeSearchResult>> SearchMedicalCodesAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchMedicalCodesAsync(q, limit, ct);
        }
        public async Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchMembersAsync(q, limit, ct);
        }

        public async Task<IReadOnlyList<MedicationSearchResult>> SearchMedicationsAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchMedicationsAsync(q, limit, ct);
        }

        public async Task<IReadOnlyList<StaffSearchResult>> SearchStaffAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchStaffAsync(q, limit, ct);
        }

        public async Task<IReadOnlyList<ProviderSearchResult>> SearchProvidersAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return await _repository.SearchProvidersAsync(q, limit, ct);
        }

        public Task<ProviderDetailResult?> GetProviderByIdAsync(int providerId, CancellationToken ct = default)
        {
            return _repository.GetProviderByIdAsync(providerId, ct);
        }

        public Task<IReadOnlyList<ClaimSearchResult>> SearchClaimsAsync(string q, int limit = 25, CancellationToken ct = default)
        {
            return _repository.SearchClaimsAsync(q, limit, ct);
        }
        public Task<ClaimDetailResult?> GetClaimByIdAsync(long memberClaimHeaderId, CancellationToken ct = default)
        {
            return _repository.GetClaimByIdAsync(memberClaimHeaderId, ct);
        }

    }

}
