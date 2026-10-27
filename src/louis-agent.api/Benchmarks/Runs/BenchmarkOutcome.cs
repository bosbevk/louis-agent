using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// A grade's or a trial's outcome as the protocol names it (F18 step 2a; ADR 0024: parsed once where it enters, with
/// <c>SettingChoice</c>). Unknown is shown and never counted as a pass; Infrastructure (F18 step 5a) is a trial that failed for the infrastructure, not the
/// agent: neither a pass nor a fail, and left out of the rates.
/// </summary>
public enum BenchmarkOutcome
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unknown")] Unknown,
    [WireName("Infrastructure")] Infrastructure,
    [WireName("Pending")] Pending,
}
