namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>
/// A mode's pass rates over its repeats so far: pass@k (passed at least once) and pass^k (passed every time), the k
/// they are over, and how many trials were left out for our own failures or run a second time. A rate is null when no
/// trial counted, never 0.
/// </summary>
public sealed record RatesInfo(double? PassAtK, double? PassHatK, int K, int Infrastructure, int Retried);
