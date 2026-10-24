namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// A grade's or a trial's outcome as the protocol names it (F18 step 2a; ADR 0024: parsed once where it enters, with
/// <c>SettingChoice</c>). Unknown is shown and never counted as a pass.
/// </summary>
public enum BenchmarkOutcome
{
    Pass,
    Fail,
    Unknown,
}
