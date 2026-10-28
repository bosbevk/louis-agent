using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Where in the agent a model was used: the agent itself, a specialist, a minion, or a grader. Which specialist it was
/// is a separate thing: two different specialists are both Specialist here.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum BenchmarkModelLevel
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today.
    // One lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Agent")] Agent,
    [WireName("Specialist")] Specialist,
    [WireName("Minion")] Minion,
    [WireName("Grader")] Grader,
}
