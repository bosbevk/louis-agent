namespace LouisAgent.Core.Tests;

/// <summary>
/// The folders on disk say what the solution says, and they say what ships. <c>src/</c> holds only the framework; a pack
/// is a tool someone takes, its checks sit apart so they can take it without them; and anything that names a product is
/// someone's own project under <c>demos/</c>.
/// </summary>
public class SolutionLayoutTests
{
    /// <summary>What marks a solution folder in the solution file, rather than a project.</summary>
    private const string SolutionFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>Each group holds the projects whose folder on disk has that name.</summary>
    [TestCase("src/engine", "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream",
        "louis-agent.composition")]
    [TestCase("src/hosts", "louis-agent.acp-server", "louis-agent.api", "louis-agent.api.contracts", "louis-agent.cli",
        "louis-agent.mcp-server")]
    [TestCase("src/web", "louis-agent.web.shared")]
    [TestCase("src/evals", "louis-agent.evals", "louis-agent.evals.checks", "louis-agent.evals.protocol",
        "louis-agent.evals.runner")]
    [TestCase("packs", "louis-agent.pack.recipes", "louis-agent.pack.shop-prices", "louis-agent.pack.where-to-buy")]
    [TestCase("packs-evals", "louis-agent.pack.recipes.checks", "louis-agent.pack.where-to-buy.checks")]
    [TestCase("demos/puls/identity", "louis-agent.identity.puls")]
    [TestCase("demos/puls/evals", "louis-agent.identity.puls.evals", "louis-agent.identity.puls.judges")]
    [TestCase("demos/puls/web", "louis-agent.puls")]
    [TestCase("demos/puls/hosts", "louis-agent.host-packs")]
    [TestCase("demos/louis-agent/web", "louis-agent.web", "louis-agent.site")]
    [TestCase("demos/louis-agent/hosts", "louis-agent.orchestrator")]
    public void AGroup_IsAFolderOnDisk(string folder, params string[] projects)
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));

        // Who sits in the group is the solution's nesting section; where each one is on disk is its own line.
        string groupId = GroupId(lines, folder);
        var nested = lines.Where(line => line.Trim().EndsWith($"= {groupId}"))
            .Select(line => line.Trim().Split(' ')[0]).ToHashSet();
        var found = lines.Where(line => line.StartsWith("Project(") && !line.Contains(SolutionFolder))
            .Where(line => nested.Contains(IdOf(line)))
            .Select(line => line.Split('"')[5].Replace('\\', '/')).ToList();

        Assert.That(found, Is.EquivalentTo(projects.Select(name => $"{folder}/{name}/{name}.csproj")));
    }

    /// <summary>
    /// The id of the solution folder at this path. Names repeat (a product has its own <c>hosts</c>), so the group is
    /// found by walking down from the top, one nesting at a time.
    /// </summary>
    private static string GroupId(string[] lines, string folder)
    {
        var folders = lines.Where(line => line.StartsWith($"Project(\"{SolutionFolder}\")")).ToList();
        string? parent = null;
        foreach (string name in folder.Split('/'))
        {
            string? above = parent;
            parent = IdOf(folders.Single(line => line.Split('"')[3] == name && ParentOf(lines, IdOf(line)) == above));
        }

        return parent!;
    }

    /// <summary>The solution folder this one is nested in, or null at the top.</summary>
    private static string? ParentOf(string[] lines, string id) =>
        lines.Select(line => line.Trim()).Where(line => line.StartsWith($"{id} = "))
            .Select(line => line.Split(" = ")[1]).SingleOrDefault();

    /// <summary>The framework's own projects never live outside <c>src/</c>, so the old top-level folders are gone.</summary>
    [TestCase("evals")]
    [TestCase("identities")]
    public void AnOldTopLevelFolder_HoldsNoProject(string folder)
    {
        string path = Path.Combine(TestPaths.RepoRoot, folder);

        var found = Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories).ToList()
            : [];

        Assert.That(found, Is.Empty);
    }

    /// <summary>The id at the end of a solution file's project line.</summary>
    private static string IdOf(string line) => line.Split('"')[^2];
}
