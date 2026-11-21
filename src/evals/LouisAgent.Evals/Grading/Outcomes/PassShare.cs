namespace LouisAgent.Evals.Grading.Outcomes;

/// <summary>A mode's pass@k and pass^k (F18 step 5a): the share of its scenarios that passed at least once, and the share that passed every time. Null when no scenario counts.</summary>
public sealed record PassShare(double? PassAtK, double? PassHatK);
