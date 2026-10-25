namespace LouisAgent.Evals.Grading;

/// <summary>One scenario's pass@k and pass^k (F18 step 5a) and the k they are over: its trials that count (an infrastructure one is left out). Null when none counts.</summary>
public sealed record PassRate(double? PassAtK, double? PassHatK, int K);
