namespace LouisAgent.Evals.Datasets;

/// <summary>
/// A dataset's <c>dataset.json</c>: its name and version, what it covers, the hash of its scenarios, and a dated line per
/// change. A change to the scenarios bumps <see cref="Version"/> (plan F18 step 3a).
/// </summary>
public sealed record DatasetManifest(
    string Name,
    int Version,
    string Covers,
    string ContentHash,
    IReadOnlyList<string> Changes);
