using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Runs.Steps;

/// <summary>
/// One step of a run, as it is saved and as a page or a person reads it: who did what, in plain words, and why.
/// </summary>
/// <param name="Summary">What happened, in one sentence a visitor can read.</param>
/// <param name="Why">Why the agent did it, in a sentence; empty for a step the code took on a rule.</param>
/// <param name="Outcome">Set on the finishing step only.</param>
/// <remarks>A record with snake_case names (ADR 0041), one JSON line per step in the file recorder.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record TriageStep(
    [property: JsonPropertyName("run_id")] string RunId,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("agent")] TriageAgent Agent,
    [property: JsonPropertyName("kind")] TriageStepKind Kind,
    [property: JsonPropertyName("summary")]
    string Summary,
    [property: JsonPropertyName("why")] string Why,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("outcome")]
    TriageOutcome? Outcome = null);
