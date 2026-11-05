using System.Text.Json.Serialization;

namespace LouisAgent.Orchestrator.Tools.Records;

/// <summary>One line of escalations.jsonl: an error handed to a human team, with why and what the error was.</summary>
/// <remarks>A named record, not an anonymous object (ADR 0041), snake_case like every orchestrator file.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record EscalationRecord(
    [property: JsonPropertyName("error_id")] string ErrorId,
    [property: JsonPropertyName("team")] string Team,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("exception_type")] string ExceptionType,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("at")] DateTimeOffset At)
{
    /// <summary>The line for handing one error to a team.</summary>
    public static EscalationRecord Of(ErrorEvent error, string team, string reason, DateTimeOffset at) =>
        new(error.Id, team, reason, error.Method, error.ExceptionType, error.Message, at);
}
