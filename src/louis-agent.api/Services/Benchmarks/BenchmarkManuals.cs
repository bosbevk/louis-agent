namespace LouisAgent.Api.Services.Benchmarks;

/// <summary>
/// A check's manual (F31 step 10b): the markdown beside its script, read for the portal's modal. The path comes from the
/// catalogue entry, never from a caller, and it must resolve inside the workspace's <c>benchmarks/</c> folder, so a
/// check whose script points elsewhere has no manual rather than a way out.
/// </summary>
public static class BenchmarkManuals
{
    public static string? Read(string workspaceRoot, BenchmarkCheck check) =>
        PathOf(workspaceRoot, check) is { } path ? File.ReadAllText(path) : null;

    public static bool Has(string workspaceRoot, BenchmarkCheck check) => PathOf(workspaceRoot, check) is not null;

    private static string? PathOf(string workspaceRoot, BenchmarkCheck check)
    {
        string folder = Path.GetFullPath(Path.Combine(workspaceRoot, "benchmarks")) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(workspaceRoot, check.ManualPath));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return path.StartsWith(folder, comparison) && File.Exists(path) ? path : null;
    }
}
