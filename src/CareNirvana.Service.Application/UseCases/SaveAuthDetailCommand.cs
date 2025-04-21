using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.UseCases
{
    public class SaveAuthDetailCommand
    {
        private readonly IAuthDetailRepository _repository;

        public SaveAuthDetailCommand(IAuthDetailRepository repository)
        {
            _repository = repository;
        }

        public async Task ExecuteAsync(AuthDetail jsonData)
        {
            var authDetail = new AuthDetail
            {
                Data = jsonData.Data,
                CreatedOn = System.DateTime.UtcNow,
                AuthNumber = jsonData.AuthNumber,
                AuthTypeId = jsonData.AuthTypeId,
                AuthClassId = jsonData.AuthClassId,
                MemberId = jsonData.MemberId,
                AuthDueDate = jsonData.AuthDueDate,
                NextReviewDate = jsonData.NextReviewDate,
                UpdatedOn = System.DateTime.UtcNow,
                DeletedOn = jsonData.DeletedOn,
                SaveType = jsonData.SaveType,
                TreatmentType = jsonData.TreatmentType,
                CreatedBy = jsonData.CreatedBy,
                UpdatedBy = jsonData.UpdatedBy,
                DeletedBy = jsonData.DeletedBy
            };

            await _repository.SaveAsync(jsonData);
        }

        public async Task<List<AuthDetail>> GetAllAsync(int memberId)
        {
            return await _repository.GetAllAsync(memberId);
        }

        public async Task<List<AuthDetail>> GetAuthData(string authNumber)
        {
            return await _repository.GetAuthData(authNumber);
        }
    }
}
