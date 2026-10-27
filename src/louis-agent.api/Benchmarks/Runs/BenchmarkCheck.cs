namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// A check the portal can run (F27): a C# eval the runner lists under its name (F18 step 4.3). Every check is one since F18 step 7.4: the Python checks, their scripts and the interpreter that ran them
/// are gone, so a check is its name, its title and where its manual is.
/// </summary>
/// <param name="Id">The name in URLs: the eval's name.</param>
/// <param name="Title">What the page calls it: what the eval proves.</param>
/// <param name="ManualPath">Where its manual is (F18 step 7.1): the path the runner's listing gave, from the repository root with forward slashes, beside the eval; null for a check with none.</param>
public sealed record BenchmarkCheck(string Id, string Title, string? ManualPath = null)
{
    /// <summary>An eval the runner lists (F18 step 4.3): its name is its id, and it brings the manual its listing named (7.1).</summary>
    public static BenchmarkCheck ForEval(string name, string title, string? manual = null) => new(name, title, manual);
}
