using LouisAgent.Evals.Contracts.Grading;

namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>The first mark a repeat didn't meet: which turn, a fact or a criterion, and the mark.</summary>
/// <param name="Turn">The turn's number, from 1.</param>
/// <param name="Kind">A fact or a criterion.</param>
/// <param name="Mark">The mark that was not met.</param>
public sealed record Miss(int Turn, MarkKind Kind, Mark Mark);
