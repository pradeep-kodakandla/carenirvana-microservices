using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IConfigAdminService
    {
        Task<JsonElement?> GetSectionData(string module, string section);
        Task<JsonElement> AddEntry(string module, string section, JsonElement newEntry);
        Task<JsonElement?> UpdateEntry(string module, string section, string id, JsonElement updatedEntry);
        Task<bool> DeleteEntry(string module, string section, string id);
    }
}
