
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Trials;

/// <summary>One run of a scenario in one mode: the transcript it left, which the graders score.</summary>
public sealed record Trial(string RunId, EvalMode Mode, int Repeat, EvalScenario Scenario, Transcript Transcript);
