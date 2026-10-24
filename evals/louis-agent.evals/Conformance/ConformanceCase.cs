using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// One scenario's reference transcripts (F18 step 4.2b): a transcript that must pass the eval's required graders, and a known-bad one
/// that must fail them. Both are cut from the protocol's own lines, or built by hand for a bad one.
/// </summary>
public sealed record ConformanceCase(string Scenario, EvalMode Mode, Transcript Reference, Transcript KnownBad);
