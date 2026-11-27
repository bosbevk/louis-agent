namespace LouisAgent.Evals.Messages.Holes.Stops;

/// <summary>
/// What a run stopped by an account limit says (F8): whose model it was, the agent's or the judge's, and what was said.
/// </summary>
public sealed record AccountLimitReachedHoles(string Whose, string Reason);
