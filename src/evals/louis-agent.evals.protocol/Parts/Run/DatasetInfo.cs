namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>
/// The dataset the run graded against, as it was then: its name, its version and the hash of its scenarios. A later
/// re-score can tell from it whether the scenarios have changed since.
/// </summary>
public sealed record DatasetInfo(string Name, int Version, string Hash);
