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
        Task<IEnumerable<codesets>> GetAllAsync(string type);
        Task<codesets?> GetByIdAsync(string id, string type);
        Task<codesets> InsertAsync(codesets entity);
        Task<codesets> UpdateAsync(codesets entity);

        Task<IReadOnlyList<CodeSearchResult>> SearchIcdAsync(string q, int limit = 25, CancellationToken ct = default);
        Task<IReadOnlyList<CodeSearchResult>> SearchMedicalCodesAsync(string q, int limit = 25, CancellationToken ct = default);
        Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(string q, int limit = 25, CancellationToken ct = default);
        Task<IReadOnlyList<MedicationSearchResult>> SearchMedicationsAsync(string q, int limit = 25, CancellationToken ct = default);
        Task<IReadOnlyList<StaffSearchResult>> SearchStaffAsync(string q, int limit = 25, CancellationToken ct = default);
        Task<IReadOnlyList<ProviderSearchResult>> SearchProvidersAsync(string q, int limit = 25, CancellationToken ct = default);

    }
}
