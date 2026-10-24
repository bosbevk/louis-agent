namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The model one level of a run ran on (F18 step 2a). <see cref="Model"/> is null when it isn't known, as an unpriced
/// request's cost is null: unknown is never a magic string.
/// </summary>
public sealed record ModelUse(string Mode, ModelLevel Level, string? Role, string? Model);
