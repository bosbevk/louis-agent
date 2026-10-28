using System.Reflection;

namespace LouisAgent.Evals.Running;

/// <summary>
/// What this build of the harness is: the commit it was built from, which the project stamps from
/// <c>-p:EvalCommit</c> (F18 step 2a). The images pass it from their <c>EVAL_COMMIT</c> build argument.
/// </summary>
public static class EvalBuild
{
    /// <summary>The commit this harness was built from, or "unknown" when the build didn't say.</summary>
    public static string Commit { get; } = CommitOf(typeof(EvalBuild).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Commit")?.Value);

    /// <summary>
    /// Reads the stamp as a commit. An empty stamp (a build argument set to nothing) says no more than a missing one,
    /// so both are "unknown".
    /// </summary>
    public static string CommitOf(string? stamped) =>
        string.IsNullOrWhiteSpace(stamped) ? "unknown" : stamped.Trim();
}
