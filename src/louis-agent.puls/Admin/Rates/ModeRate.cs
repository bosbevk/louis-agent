namespace LouisAgent.Puls.Admin.Rates;

/// <summary>
/// A mode's pass@k and pass^k as the run's mode rows carry them (F18 step 5a): the runner's numbers, read and shown,
/// never worked out here. <see cref="K"/> is the repeats they are over; null for a run from before the rates, as is
/// each rate where no trial counted (shown as a dash, never 0).
/// </summary>
public sealed record ModeRate(string Mode, int? K, double? PassAtK, double? PassHatK, int Infrastructure, int Retried);
