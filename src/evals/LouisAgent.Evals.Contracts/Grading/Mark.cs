namespace LouisAgent.Evals.Contracts.Grading;

/// <summary>
/// One mark on a turn, the same for a fact and for a criterion: what was checked, whether it was met, and why, in one
/// plain line, so a red mark explains itself.
/// </summary>
/// <param name="Text">The fact's words or the criterion, as the eval file has it.</param>
/// <param name="Met">Whether it was met.</param>
/// <param name="Reason">Why: what code saw, or what the judge said.</param>
public sealed record Mark(string Text, bool Met, string Reason);
