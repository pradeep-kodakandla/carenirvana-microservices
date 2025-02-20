using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            };

            await _repository.SaveAsync(authDetail);
        }
    }
}
