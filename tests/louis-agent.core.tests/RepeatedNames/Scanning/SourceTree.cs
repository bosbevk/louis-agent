namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>Where the repo's sources and built projects are, for the ratchets that read them (F18 step 7.5).</summary>
public static class SourceTree
{
    public static readonly string[] Roots =
    [
        "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream", "identities", "packs", "src", "tools",
        "evals", "tests"
    ];

    /// <summary>Every <c>.cs</c> and <c>.razor</c> file under the roots, bin, obj and published folders left out.</summary>
    public static IEnumerable<string> Files() =>
        Roots.Select(r => Path.Combine(TestPaths.RepoRoot, r)).Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".razor"))
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(p => p is "bin" or "obj" or "node_modules" or "wwwroot"));

    public static string Relative(string path) => Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/');

    /// <summary>The compiled assembly of each project that is not a test project (its newest build), when the solution has been built.</summary>
    public static IReadOnlyList<string> BuiltAssemblies()
    {
        var projects = new List<string> { "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream" };
        foreach (string group in new[] { "identities", "packs", "src", "tools", "evals" })
        {
            string dir = Path.Combine(TestPaths.RepoRoot, group);
            if (Directory.Exists(dir))
                projects.AddRange(Directory.GetDirectories(dir)
                    .Select(d => Path.GetRelativePath(TestPaths.RepoRoot, d).Replace('\\', '/')));
        }

        return
        [
            .. projects.Select(p => Newest(Path.Combine(TestPaths.RepoRoot, p), Path.GetFileName(p)))
                .Where(p => p is not null).Select(p => p!)
        ];
    }

    private static string? Newest(string projectDir, string assemblyName)
    {
        string bin = Path.Combine(projectDir, "bin");
        return Directory.Exists(bin)
            ? Directory.EnumerateFiles(bin, assemblyName + ".dll", SearchOption.AllDirectories)
                .Where(f => !f.Contains("BlazorDebugProxy")).OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;
    }
}
