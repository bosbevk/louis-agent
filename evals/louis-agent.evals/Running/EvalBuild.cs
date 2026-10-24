using System.Reflection;

namespace LouisAgent.Evals.Running;

/// <summary>What this build of the harness is: the commit it was built from, stamped by the project (F18 step 2a).</summary>
public static class EvalBuild
{
    /// <summary>The commit, or "unknown" when the build didn't stamp one.</summary>
    public static string Commit { get; } = typeof(EvalBuild).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Commit")?.Value ?? "unknown";
}
