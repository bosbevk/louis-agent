using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;

namespace LouisAgent.Evals.Trials;

/// <summary>One run of a scenario in one mode: the transcript it left, which the checks score.</summary>
public sealed record Trial(string RunId, EvalMode Mode, int Repeat, EvalScenario Scenario, Transcript Transcript);
