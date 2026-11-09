using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>
/// One step of a triage run as the demo shows it (F106.3.4): who took it, what kind it is, what it did and why, and
/// the case's outcome on the step that closes it. The pack's own step, read from its JSON; this project may not
/// reference the pack, so it keeps this copy, and <c>TriageRunFileTests</c> holds the two together.
/// </summary>
/// <param name="RunId">The run it belongs to.</param>
/// <param name="Number">Its place in the run, from 1.</param>
/// <param name="Agent">Who took it: code, sorter, manager, investigator, coder or reviewer.</param>
/// <param name="Kind">What kind of step it is: collected, sorted, decided and the rest.</param>
/// <param name="Summary">What it did, in a sentence.</param>
/// <param name="Why">Its reason, in its own words.</param>
/// <param name="At">When it was saved, in UTC.</param>
/// <param name="Outcome">How the case ended, on the step that closes it.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed record TriageStepView(
    [property: JsonPropertyName("run_id")] string RunId,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("agent")] string Agent,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("summary")]
    string Summary,
    [property: JsonPropertyName("why")] string Why,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("outcome")]
    string? Outcome = null);
