using LouisAgent.Core.Web;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Web.Search;
using NUnit.Framework;

namespace LouisAgent.Testing;

/// <summary>The repo's paths and the options a test builds an engine with, the same from every test project.</summary>
public static class TestPaths
{
    /// <summary>The folder holding the solution, found upward, so a test project can sit at any depth.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Walks up from the test's build output until it reaches the solution file.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "louis-agent-solution.sln"))) dir = dir.Parent!;

        return dir.FullName;
    }

    /// <summary>
    /// Whether a repo-relative path lies in a test project. Tests sit with what they prove, under <c>src/tests</c>, a
    /// demo or <c>packs/tests</c>, so it's the project's name that says so and not a top-level folder.
    /// </summary>
    public static bool InTestProject(string relativePath) =>
        relativePath.Split('/').SkipLast(1).Any(folder => folder.EndsWith(".tests", StringComparison.Ordinal)
                                                          || folder.EndsWith(".integration-tests",
                                                              StringComparison.Ordinal)
                                                          || folder.EndsWith(".testing", StringComparison.Ordinal)
                                                          || folder.EndsWith(".standin", StringComparison.Ordinal));

    /// <summary>
    /// The demos, one folder each under <c>demos/</c> (or under <paramref name="folder"/>), in order of name. A folder
    /// whose name starts with a dot is what a run leaves there for a moment (the starter's spawn test makes
    /// <c>demos/.spawn-…</c>), never a demo, so a test counting demos at the same time doesn't count it (FX131).
    /// </summary>
    public static IReadOnlyList<string> Demos(string? folder = null) =>
    [
        .. Directory.EnumerateDirectories(folder ?? Path.Combine(RepoRoot, "demos"))
            .Where(demo => !Path.GetFileName(demo).StartsWith('.'))
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>A small skills file for a test that needs an engine with something loaded; no host reads it.</summary>
    public static string SampleSkills { get; } =
        Path.Combine(RepoRoot, "src/tests/shared/louis-agent.testing/Fixtures", "SampleSkills.md");

    /// <summary>An empty skills folder of the tests' own, since the real ones belong to the demos.</summary>
    public static string Skills { get; } =
        Path.Combine(RepoRoot, "src/tests/shared/louis-agent.testing/Fixtures/skills");

    /// <summary>The built-in prompts in core (CL8 step 4), embedded and read only from the assembly.</summary>
    public static string Prompts { get; } =
        Path.Combine(RepoRoot, "src", "engine", "louis-agent.core", EmbeddedPrompts.Folder);

    public static AgentOptions Agent() => new()
    {
        WorkspaceRoot = RepoRoot,
        // No network in tests: AgentHost.Build would otherwise fetch exchange rates from Frankfurter (F14).
        RatesUrl = null,
    };

    /// <summary>
    /// The tool objects of the named toolsets, core's or the fake pack's, built as a session's are (F5: an engine has
    /// only the toolsets it is given). <paramref name="webSearch"/> replaces the configured search provider.
    /// </summary>
    public static IReadOnlyList<object> Toolsets(AgentOptions options, IEnumerable<string> names,
        IWebSearchProvider? webSearch = null)
    {
        var context = new ToolsetContext(options,
            webSearch is null ? WebAccess.Create(options) : WebAccess.Create(options, null, webSearch));
        return ToolsetCatalog.BuildTools(TestTools.Catalog.Resolve(names, context), context);
    }
}
