using CareNirvana.Service.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Services
{
    public class ConfigAdminService : IConfigAdminService
    {
        private readonly IConfigAdminRepository _repository;

        public ConfigAdminService(IConfigAdminRepository repository)
        {
            _repository = repository;
        }


        public async Task<JsonElement?> GetSectionData(string module, string section)
        {
            return await _repository.GetSectionData(module, section);
        }


        public async Task<JsonElement> AddEntry(string module, string section, JsonElement newEntry)
        {
            return await _repository.AddEntry(module, section, newEntry);
        }


        public async Task<JsonElement?> UpdateEntry(string module, string section, string id, JsonElement updatedEntry)
        {
            return await _repository.UpdateEntry(module, section, id, updatedEntry);
        }


        public async Task<bool> DeleteEntry(string module, string section, string id)
        {
            return await _repository.DeleteEntry(module, section, id);
        }
    }
}
