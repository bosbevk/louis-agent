namespace LouisAgent.Web.Evals.Client;

/// <summary>How a start went: the run's id when the host started it, or the host's own reason when it didn't.</summary>
/// <param name="Run">The started run's id, or null when the start was refused.</param>
/// <param name="Refused">The host's reason, word for word, or null when the run started.</param>
public sealed record RunStart(string? Run, string? Refused);
