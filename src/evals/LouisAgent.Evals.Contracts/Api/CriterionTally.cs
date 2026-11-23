namespace LouisAgent.Evals.Contracts.Api;

/// <summary>
/// A criterion by its words, and in how many runs it was missed and in how many a person flipped it. On one run's row
/// each is 0 or 1; an eval's history adds its rows' up.
/// </summary>
/// <param name="Text">The criterion's words.</param>
/// <param name="Missed">In how many runs it counted as not met.</param>
/// <param name="Flipped">In how many runs a person flipped it.</param>
public sealed record CriterionTally(string Text, int Missed, int Flipped);
