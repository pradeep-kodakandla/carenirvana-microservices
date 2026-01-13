using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface ICfgvalidationRepository
    {
        Task<IEnumerable<Cfgvalidation>> GetAllAsync(int moduleId);
        Task<Cfgvalidation?> GetByIdAsync(int validationId);

        Task<Cfgvalidation> InsertAsync(Cfgvalidation entity);
        Task<Cfgvalidation> UpdateAsync(Cfgvalidation entity);

        // Soft delete: sets activeFlag=false and stamps deletedOn/deletedBy
        Task<bool> DeleteAsync(int validationId, int deletedBy);

        Task<string?> GetPrimaryTemplateJsonAsync(int moduleId);
    }
}
