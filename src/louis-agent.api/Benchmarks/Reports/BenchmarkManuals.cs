using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// An eval's manual (F31 step 10b; beside the eval since F18 step 7.1): the markdown the runner's listing named, read for the portal's modal. The path comes from the
/// catalogue entry, never from a caller, and it must resolve inside the workspace's <c>evals/</c> folder, so an entry whose path points elsewhere has no manual rather
/// than a way out.
/// </summary>
public static class BenchmarkManuals
{
    public static string? Read(string workspaceRoot, BenchmarkCheck check) =>
        PathOf(workspaceRoot, check) is { } path ? File.ReadAllText(path) : null;

    public static bool Has(string workspaceRoot, BenchmarkCheck check) => PathOf(workspaceRoot, check) is not null;

    private static string? PathOf(string workspaceRoot, BenchmarkCheck check)
    {
        if (check.ManualPath is not { Length: > 0 } relative) return null;
        string folder = Path.GetFullPath(Path.Combine(workspaceRoot, "evals")) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(workspaceRoot, relative));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return path.StartsWith(folder, comparison) && File.Exists(path) ? path : null;
    }
}
