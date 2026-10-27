using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// One scenario's references (F18 step 4.2b): a transcript that must pass the eval's required graders, and known-bad ones, each wrong in
/// one way, of which every required grader must fail at least one.
/// </summary>
public sealed record ConformanceCase(
    string Scenario,
    EvalMode Mode,
    Transcript Reference,
    IReadOnlyList<KnownBad> KnownBad);
