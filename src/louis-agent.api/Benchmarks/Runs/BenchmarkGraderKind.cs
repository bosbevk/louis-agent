namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>What a grader judges with, as the protocol names it (F18 step 2a; parsed once, with <c>SettingChoice</c>).</summary>
public enum BenchmarkGraderKind
{
    FixedRule,
    Model,
    Human,
}
