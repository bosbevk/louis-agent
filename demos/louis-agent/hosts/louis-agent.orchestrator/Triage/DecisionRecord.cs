using System.Text.Json.Serialization;
using LouisAgent.Choices;
using LouisAgent.Orchestrator.Comms;

namespace LouisAgent.Orchestrator.Triage;

/// <summary>
/// One line of decisions.jsonl: what was decided about an error, and (F3) what it cost, triage and fix together and
/// each part. The keys are snake_case like every orchestrator file (fixes.jsonl, escalations.jsonl), and the usage
/// parts are null when they weren't measured.
/// </summary>
/// <remarks>
/// A named record instead of the anonymous object it was (ADR 0041), so the file's shape is written down in one place
/// and a test can hold it. The usage parts are <see cref="UsageLine"/>s, the same shape fixes.jsonl uses.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record DecisionRecord(
    [property: JsonPropertyName("error_id")] string ErrorId,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("usage")] UsageLine? Usage,
    [property: JsonPropertyName("triage_usage")] UsageLine? TriageUsage,
    [property: JsonPropertyName("louis_agent_usage")] UsageLine? LouisAgentUsage)
{
    /// <summary>The line for one decision about one error, with what it used in the charged currency.</summary>
    public static DecisionRecord Of(Decision decision, ErrorEvent error, EventUsage used, string currency,
        DateTimeOffset at) =>
        new(error.Id, error.Method, decision.Outcome.Wire(), decision.Reason, at,
            UsageLine.Of(used.Total, currency),
            UsageLine.Of(used.Triage, currency),
            UsageLine.Of(used.LouisAgent, currency));
}
