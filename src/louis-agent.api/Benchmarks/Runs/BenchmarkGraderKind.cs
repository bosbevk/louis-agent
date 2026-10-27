using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>What a grader judges with, as the protocol names it (F18 step 2a; parsed once, with <c>SettingChoice</c>).</summary>
public enum BenchmarkGraderKind
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("FixedRule")] FixedRule,
    [WireName("Model")] Model,
    [WireName("Human")] Human,
}
