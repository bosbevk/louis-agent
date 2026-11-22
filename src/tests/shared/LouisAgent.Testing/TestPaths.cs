using LouisAgent.Core.Web;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Web.Search;
using LouisAgent.Composition.Messages;
using LouisAgent.Testing.Messages;
using LouisAgent.Testing.Messages.Holes;
using NUnit.Framework;

namespace LouisAgent.Testing;

/// <summary>The repo's paths and the options a test builds an engine with, the same from every test project.</summary>
public static class TestPaths
{
    /// <summary>
    /// The folder holding the solution, found upward from the test's build output, so a test project can sit at any
    /// depth, in this repository or in a product's of its own (F166.3).
    /// </summary>
    public static string RepoRoot { get; } = RootAbove(TestContext.CurrentContext.TestDirectory);

    /// <summary>
    /// The nearest folder at or above this one that holds a solution file, whatever it is called: a repository's root
    /// has one, and a test project never sits below a second.
    /// </summary>
    public static string RootAbove(string folder)
    {
        if (TryRootAbove(folder, out string root)) return root;

        var start = new NoSolutionAboveHoles(folder);
        throw new InvalidOperationException(MessageText.Format(TestingText.NoSolutionAbove, start));
    }

    /// <summary>Whether a solution file sits at or above this folder, and the folder that holds it.</summary>
    public static bool TryRootAbove(string folder, out string root)
    {
        for (var dir = new DirectoryInfo(folder); dir is not null; dir = dir.Parent)
        {
            if (!dir.EnumerateFiles("*.sln").Any() && !dir.EnumerateFiles("*.slnx").Any()) continue;

            root = dir.FullName;
            return true;
        }

        root = "";
        return false;
    }

    /// <summary>
    /// Whether a repo-relative path lies in a test project. Tests sit with what they prove, under <c>src/tests</c>, a
    /// demo or <c>packs/tests</c>, so it's the project's name that says so and not a top-level folder.
    /// </summary>
    public static bool InTestProject(string relativePath) =>
        relativePath.Split('/').SkipLast(1).Any(folder => folder.EndsWith(".Tests", StringComparison.Ordinal)
                                                          || folder.EndsWith(".IntegrationTests",
                                                              StringComparison.Ordinal)
                                                          || folder.EndsWith(".Testing", StringComparison.Ordinal)
                                                          || folder.EndsWith(".StandIn", StringComparison.Ordinal));

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
        Path.Combine(RepoRoot, "src/tests/shared/LouisAgent.Testing/Fixtures", "SampleSkills.md");

    /// <summary>An empty skills folder of the tests' own, since the real ones belong to the demos.</summary>
    public static string Skills { get; } =
        Path.Combine(RepoRoot, "src/tests/shared/LouisAgent.Testing/Fixtures/skills");

    /// <summary>
    /// Core's project folder, where its built-in prompts' folders sit (CL8 step 4, CL68.1), embedded and read only
    /// from the assembly.
    /// </summary>
    public static string Prompts { get; } = Path.Combine(RepoRoot, "src", "engine", "LouisAgent.Core");

    /// <summary>The start of the line a project embeds its prompt folders with (CL68.1).</summary>
    private const string EmbeddedLine = "<EmbeddedResource Include=\"";

    /// <summary>The end of each folder's pattern on that line: <c>agents\**\*.md</c>.</summary>
    private const string FolderPattern = "\\**\\*.md";

    /// <summary>
    /// A project's prompt folders, as its project file embeds them (CL68.1: the project file names them, so they are
    /// read from there, never listed twice): <c>agents</c>, <c>toolsets</c>, <c>shared</c> and the like. None for a
    /// project that embeds no prompts.
    /// </summary>
    public static IReadOnlyList<string> PromptFolders(string project)
    {
        var line = Directory.EnumerateFiles(project, "*.csproj").SelectMany(File.ReadLines)
            .Select(text => text.Trim())
            .FirstOrDefault(text => text.StartsWith(EmbeddedLine, StringComparison.Ordinal)
                                    && text.Contains(FolderPattern, StringComparison.Ordinal));
        if (line is null) return [];

        string include = line[EmbeddedLine.Length..line.IndexOf('"', EmbeddedLine.Length)];
        return [.. include.Split(';').Select(pattern => pattern.Replace(FolderPattern, ""))];
    }

    /// <summary>A project's prompts, each by its path in the project with forward slashes, in order.</summary>
    public static IReadOnlyList<string> PromptFiles(string project) =>
    [
        .. PromptFolders(project)
            .Select(folder => Path.Combine(project, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(project, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
    ];

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
