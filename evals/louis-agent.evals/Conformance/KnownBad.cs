using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// A transcript that is wrong on purpose, named by its label (its file's name between the scenario and the extension). Each required
/// grader must fail at least one of them (F18 step 4.2b).
/// </summary>
public sealed record KnownBad(string Label, Transcript Transcript);
