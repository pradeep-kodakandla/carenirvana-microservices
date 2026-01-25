using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public sealed class RuleGroupDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool ActiveFlag { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; }
        public long? CreatedBy { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }
    }


    public sealed class RuleDto
    {
        public long Id { get; set; }
        public long RuleGroupId { get; set; }

        public string Name { get; set; } = "";
        public string RuleType { get; set; } = "";     // REALTIME, BATCH...
        public string Description { get; set; } = "";

        public string? RuleJson { get; set; }          // jsonb -> text
        public bool ActiveFlag { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; }
        public long? CreatedBy { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }
    }


    public sealed class UpsertRuleGroupRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool ActiveFlag { get; set; } = true;
    }


    public sealed class UpsertRuleRequest
    {
        public long RuleGroupId { get; set; }
        public string Name { get; set; } = "";
        public string RuleType { get; set; } = "";
        public string Description { get; set; } = "";
        public string? RuleJson { get; set; }
        public bool ActiveFlag { get; set; } = true;
    }


    public sealed class DecisionTableParamDto
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "input"; // input | output
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string DataType { get; set; } = "string";   // string, number, boolean, date, datetime, enum
        public string InputType { get; set; } = "text";    // text, number, date, datetime, select, toggle
        public List<string> Operators { get; set; } = new();
        public List<string> Options { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class DecisionTableListDto
    {
        public string Id { get; set; } = "";                 // uniquedecisiontableid
        public string Name { get; set; } = "";               // ruledecisiontablename
        public string Status { get; set; } = "";             // deploymentstatus
        public int Version { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public bool ActiveFlag { get; set; } = true;
    }


    public sealed class DecisionTablePayloadDto
    {
        public string Id { get; set; } = "";                 // uniquedecisiontableid
        public string Name { get; set; } = "";               // ruledecisiontablename
        public string Description { get; set; } = "";
        public string HitPolicy { get; set; } = "FIRST";
        public int Version { get; set; } = 1;
        public string Status { get; set; } = "DRAFT";        // deploymentstatus
        public bool ActiveFlag { get; set; } = true;

        public string UpdatedOn { get; set; } = "";          // keep as ISO string for UI consistency

        // Your decision table json (full payload) is stored in decisiontablejson.
        // Keep these if your Angular builder uses them, but you’ll store the whole thing as JSON in DB.
        public object? Columns { get; set; }
        public object? Rows { get; set; }
    }

    public sealed class RuleDataFieldDto
    {
        public long RuleDataFieldId { get; set; }
        public long ModuleId { get; set; }
        public string ModuleName { get; set; } = "";

        // store jsonb as text for easy UI use (you can JSON.parse in Angular)
        public string RuleDataFieldJson { get; set; } = "{}";

        public bool ActiveFlag { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; }
        public long? CreatedBy { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }
    }


    public sealed class RuleDataFunctionDto
    {
        public long RuleDataFunctionId { get; set; }
        public string RuleDataFunctionName { get; set; } = "";
        public string Description { get; set; } = "";
        public string DeploymentStatus { get; set; } = "";
        public int Version { get; set; }
        public string RuleDataFunctionJson { get; set; } = "{}"; // stored as text for API/repo, cast to jsonb in SQL
        public bool ActiveFlag { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public long? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedOn { get; set; }
        public long? DeletedBy { get; set; }
    }

    public sealed class RuleDataFunctionListDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string DeploymentStatus { get; set; } = "";
        public int Version { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public bool ActiveFlag { get; set; }
    }

    public sealed class UpsertRuleDataFunctionRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string DeploymentStatus { get; set; } = "DRAFT";
        public int Version { get; set; } = 1;

        // Accept arbitrary JSON from API callers
        public JsonElement RuleDataFunctionJson { get; set; }

        public bool ActiveFlag { get; set; } = true;
    }

    public sealed class DashboardKpiDto
    {
        public long Value { get; set; }
        public string Sub { get; set; } = "";
    }

    public sealed class RulesDashboardStatsDto
    {
        public DashboardKpiDto ActiveRules { get; set; } = new();
        public DashboardKpiDto RuleGroups { get; set; } = new();
        public DashboardKpiDto DataFunctions { get; set; } = new();
        public DashboardKpiDto RecordsProcessed { get; set; } = new(); // keep static for now (no source yet)
    }

    public sealed class RulesDashboardCountsRow
    {
        public long ActiveRules { get; set; }
        public long RuleGroupsTotal { get; set; }
        public long RuleGroupsActive { get; set; }
        public long DataFunctionsTotal { get; set; }
        public long DataFunctionsActive { get; set; }
    }

}
