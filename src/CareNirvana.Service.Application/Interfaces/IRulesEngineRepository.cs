using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IRulesEngineRepository
    {
        Task<IReadOnlyList<RuleGroupDto>> GetRuleGroupsAsync();
        Task<long> CreateRuleGroupAsync(UpsertRuleGroupRequest req, long? userId = null);
        Task UpdateRuleGroupAsync(long id, UpsertRuleGroupRequest req, long? userId = null);
        Task SoftDeleteRuleGroupAsync(long id, long? userId = null);

        // rulesengine.cfgrule
        Task<IReadOnlyList<RuleDto>> GetRulesAsync(long? ruleGroupId = null);
        Task<long> CreateRuleAsync(UpsertRuleRequest req, long? userId = null);
        Task UpdateRuleAsync(long id, UpsertRuleRequest req, long? userId = null);
        Task SoftDeleteRuleAsync(long id, long? userId = null);

        // rulesengine.cfgruledecisiontable
        Task<IReadOnlyList<DecisionTableListDto>> GetDecisionTablesAsync();
        Task<string?> GetDecisionTableJsonAsync(string id);
        Task<string> CreateDecisionTableAsync(
            string json,
            string id,
            string name,
            string description,
            string hitPolicy,
            string status,
            int version,
            long? userId = null);

        Task UpdateDecisionTableAsync(
            string id,
            string json,
            string name,
            string description,
            string hitPolicy,
            string status,
            int version,
            long? userId = null);
        Task<RuleDto?> GetRealtimeRuleByDecisionTableIdAsync(string decisionTableId);
        Task UpdateRuleJsonAsync(long ruleId, string ruleJson, long? userId = null);

        Task SoftDeleteDecisionTableAsync(string id, long? userId = null);

        public Task<IReadOnlyList<RuleDataFieldDto>> GetRuleDataFieldsAsync(long? moduleId = null);
        public Task<string?> GetRuleDataFieldJsonAsync(long ruleDataFieldId);



        Task<IReadOnlyList<RuleDataFunctionListDto>> GetRuleDataFunctionsAsync();
        Task<RuleDataFunctionDto?> GetRuleDataFunctionAsync(long id);
        Task<string?> GetRuleDataFunctionJsonAsync(long id);

        Task<long> CreateRuleDataFunctionAsync(UpsertRuleDataFunctionRequest req, long? userId = null);
        Task UpdateRuleDataFunctionAsync(long id, UpsertRuleDataFunctionRequest req, long? userId = null);
        Task SoftDeleteRuleDataFunctionAsync(long id, long? userId = null);

        Task<RulesDashboardCountsRow> GetDashboardCountsAsync();




        Task<IReadOnlyList<TriggerRuleRow>> GetActiveRulesForTriggerAsync(string triggerKey);
        Task InsertRuleExecutionLogAsync(object logRow);


        Task<IReadOnlyList<RuleActionDto>> GetRuleActionsAsync(bool? activeOnly = null);
        Task<RuleActionDto?> GetRuleActionAsync(long id);


        Task<RulePagedResult<RuleExecutionLogListItemDto>> GetRuleExecutionLogsAsync(int page, int pageSize);
    }
}
