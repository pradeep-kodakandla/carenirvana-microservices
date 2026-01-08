using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public sealed class RuleGroupDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string ScheduleType { get; set; } = ""; // DailyOnce, DailyHourly...
        public string Description { get; set; } = "";
        public string Purpose { get; set; } = "";
    }

    public sealed class RuleDto
    {
        public long Id { get; set; }
        public long RuleGroupId { get; set; }
        public string Name { get; set; } = "";
        public string RuleType { get; set; } = "";     // REALTIME, BATCH...
        public string Description { get; set; } = "";
        public string? RuleJson { get; set; }          // optional JSON text
    }

    public sealed class UpsertRuleGroupRequest
    {
        public string Name { get; set; } = "";
        public string ScheduleType { get; set; } = "";
        public string Description { get; set; } = "";
        public string Purpose { get; set; } = "";
    }

    public sealed class UpsertRuleRequest
    {
        public long RuleGroupId { get; set; }
        public string Name { get; set; } = "";
        public string RuleType { get; set; } = "";
        public string Description { get; set; } = "";
        public string? RuleJson { get; set; }
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

    public sealed class DecisionTableTemplateDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string HitPolicy { get; set; } = "FIRST";
        public int Version { get; set; } = 1;
        public string Status { get; set; } = "DRAFT";
        public string UpdatedOn { get; set; } = ""; // ISO string

        public List<DecisionTableParamDto> Inputs { get; set; } = new();
        public List<DecisionTableParamDto> Outputs { get; set; } = new();

        // template builder stores no rows, but keep it for future compatibility
        public List<object> Rows { get; set; } = new();
    }

    public sealed class DecisionTableTemplateListDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public int Version { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }

    public sealed class DecisionTableListDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public int Version { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }

    public sealed class DecisionTablePayloadDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string HitPolicy { get; set; } = "FIRST";
        public int Version { get; set; } = 1;
        public string Status { get; set; } = "DRAFT";
        public string UpdatedOn { get; set; } = "";

        public object? Columns { get; set; }   // store as JSON
        public object? Rows { get; set; }      // store as JSON
    }


}
