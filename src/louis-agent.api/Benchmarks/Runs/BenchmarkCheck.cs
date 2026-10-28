namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// An eval the portal can run, under its name: what it proves is its title, its manual sits where the runner said, and
/// it keeps the runner's whole line for the eval list (its graders, dataset and categories).
/// </summary>
/// <param name="Id">The name in URLs: the eval's name.</param>
/// <param name="Title">What the page calls it: what the eval proves.</param>
/// <param name="ManualPath">
/// Where its manual is, from the repository root with forward slashes, beside the eval; null for a check with none.
/// </param>
/// <param name="Eval">The runner's line for it; null for a check a test makes by hand.</param>
public sealed record BenchmarkCheck(string Id, string Title, string? ManualPath = null, EvalLine? Eval = null);
