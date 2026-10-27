using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The level a model is used at, as the protocol names it (F18 step 2a; parsed once, with <c>SettingChoice</c>). A specialist's
/// role is separate: two specialists are both Specialist.
/// </summary>
public enum BenchmarkModelLevel
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Agent")] Agent,
    [WireName("Specialist")] Specialist,
    [WireName("Minion")] Minion,
    [WireName("Grader")] Grader,
}
