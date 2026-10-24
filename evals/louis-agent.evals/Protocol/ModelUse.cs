namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The model one level of a run ran on (F18 step 2a): the mode, the level (agent, specialist, minion or grader) and the
/// model, "unknown" when the harness doesn't know it.
/// </summary>
public sealed record ModelUse(string Mode, string Level, string Model);
