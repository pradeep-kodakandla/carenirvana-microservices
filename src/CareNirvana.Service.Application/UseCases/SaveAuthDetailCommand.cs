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

        public async Task<long?> ExecuteAsync(AuthDetail jsonData)
        {
            var insertedId = await _repository.SaveAsync(jsonData);
            return insertedId;
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
