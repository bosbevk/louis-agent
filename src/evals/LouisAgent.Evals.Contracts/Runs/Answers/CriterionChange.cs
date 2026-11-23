namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>A criterion added to a turn since the run before, or taken out of it.</summary>
/// <param name="Conversation">The conversation's id.</param>
/// <param name="Turn">The turn, from 1.</param>
/// <param name="Criterion">The criterion's words.</param>
/// <param name="Added">Added since the run before, or else taken out.</param>
public sealed record CriterionChange(string Conversation, int Turn, string Criterion, bool Added);
