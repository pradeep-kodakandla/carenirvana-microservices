using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IRulesEngineRepository
    {
        Task<IReadOnlyList<RuleGroupDto>> GetRuleGroupsAsync();
        Task<long> CreateRuleGroupAsync(UpsertRuleGroupRequest req, long? userId = null);
        Task UpdateRuleGroupAsync(long id, UpsertRuleGroupRequest req, long? userId = null);
        Task SoftDeleteRuleGroupAsync(long id, long? userId = null);

        Task<IReadOnlyList<RuleDto>> GetRulesAsync(long? ruleGroupId = null);
        Task<long> CreateRuleAsync(UpsertRuleRequest req, long? userId = null);
        Task UpdateRuleAsync(long id, UpsertRuleRequest req, long? userId = null);
        Task SoftDeleteRuleAsync(long id, long? userId = null);

        Task<IReadOnlyList<DecisionTableListDto>> GetDecisionTablesAsync();
        Task<string?> GetDecisionTableJsonAsync(string id);
        Task<string> CreateDecisionTableAsync(string json, string id, string name, string description, string hitPolicy, string status, int version, long? userId = null);
        Task UpdateDecisionTableAsync(string id, string json, string name, string description, string hitPolicy, string status, int version, long? userId = null);
        Task SoftDeleteDecisionTableAsync(string id, long? userId = null);


    }
}
