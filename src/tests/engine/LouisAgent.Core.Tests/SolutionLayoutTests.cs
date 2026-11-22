namespace LouisAgent.Core.Tests;

/// <summary>
/// The folders on disk say what the solution says, and they say what ships. <c>src/</c> holds only the framework; a
/// pack is a tool someone takes, its checks sit apart so they can take it without them; and anything that names a
/// product is someone's own project under <c>demos/</c>.
/// </summary>
public class SolutionLayoutTests
{
    /// <summary>The one dot folder that is in the solution: where Claude's instructions live.</summary>
    private const string InstructionsFolder = ".claude";

    /// <summary>What marks a solution folder in the solution file, rather than a project.</summary>
    private const string SolutionFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>Each group holds the projects whose folder on disk has that name.</summary>
    [TestCase("src/engine", "LouisAgent.Core", "LouisAgent.Choices", "LouisAgent.SessionStream",
        "LouisAgent.Composition", "LouisAgent.Core.Web", "LouisAgent.Core.Telemetry", "LouisAgent.Core.Anthropic",
        "LouisAgent.Core.Ollama", "LouisAgent.Core.OpenAi")]
    [TestCase("src/hosts", "LouisAgent.Acp", "LouisAgent.Api", "LouisAgent.Api.Contracts", "LouisAgent.Cli",
        "LouisAgent.Mcp", "LouisAgent.Accounts", "LouisAgent.Accounts.Contracts", "LouisAgent.Api.Evals")]
    [TestCase("src/web", "LouisAgent.Web", "LouisAgent.Web.Accounts", "LouisAgent.Web.Themes",
        "LouisAgent.Web.Framework", "LouisAgent.Web.Docs", "LouisAgent.Web.Places",
        "LouisAgent.Diagrams", "LouisAgent.Diagrams.Engine")]
    [TestCase("src/evals", "LouisAgent.Evals", "LouisAgent.Evals.Checks", "LouisAgent.Evals.Protocol",
        "LouisAgent.Evals.Runner")]
    [TestCase("packs/tools", "LouisAgent.Packs.Web", "LouisAgent.Packs.Shell", "LouisAgent.Packs.Files",
        "LouisAgent.Packs.Git", "LouisAgent.Packs.DotNet", "LouisAgent.Packs.Calculator", "LouisAgent.Packs.Docs",
        "LouisAgent.Packs.Triage", "LouisAgent.Packs.Leads",
        "LouisAgent.Packs.Site")]
    [TestCase("packs/web", "LouisAgent.Packs.Files.Web", "LouisAgent.Packs.Git.Web")]
    [TestCase("demos/starter/web", "LouisAgent.Starter")]
    [TestCase("demos/starter/hosts", "LouisAgent.Starter.Api")]
    [TestCase("demos/starter/tests", "LouisAgent.Starter.Tests", "LouisAgent.Starter.IntegrationTests")]
    [TestCase("demos/louis-agent/identity", "LouisAgent.Identities.Coding", "LouisAgent.Identities.Triage")]
    [TestCase("src/site", "LouisAgent.Site", "LouisAgent.Identities.Guide", "LouisAgent.SiteApi",
        "LouisAgent.Identities.Showroom", "LouisAgent.SiteFramework", "LouisAgent.SiteFrameworkApi",
        "LouisAgent.Identities.Framework")]
    [TestCase("src/tests/site", "LouisAgent.Site.Tests", "LouisAgent.SiteFramework.Tests",
        "LouisAgent.Identities.Framework.Tests")]
    [TestCase("demos/louis-agent/web", "LouisAgent.CodeReview")]
    [TestCase("demos/louis-agent/hosts", "LouisAgent.Orchestrator", "LouisAgent.CodingCli",
        "LouisAgent.CodingAcp", "LouisAgent.CodingMcp", "LouisAgent.CodingApi", "LouisAgent.Coding.Catalog")]
    [TestCase("src/tests/engine", "LouisAgent.Core.Tests", "LouisAgent.Core.IntegrationTests",
        "LouisAgent.Choices.Tests", "LouisAgent.SessionStream.Tests", "LouisAgent.Core.Web.Tests",
        "LouisAgent.Core.Telemetry.Tests", "LouisAgent.Core.Anthropic.Tests", "LouisAgent.Core.Ollama.Tests",
        "LouisAgent.Core.OpenAi.Tests")]
    [TestCase("src/tests/hosts", "LouisAgent.Acp.Tests", "LouisAgent.Mcp.Tests",
        "LouisAgent.Accounts.Tests", "LouisAgent.Accounts.IntegrationTests", "LouisAgent.Api.Tests",
        "LouisAgent.Api.IntegrationTests", "LouisAgent.Api.Testing")]
    [TestCase("src/tests/web", "LouisAgent.Web.Tests", "LouisAgent.Web.Testing", "LouisAgent.Web.Accounts.Tests",
        "LouisAgent.Web.Themes.Tests", "LouisAgent.Web.Framework.Tests",
        "LouisAgent.Web.Docs.Tests", "LouisAgent.Web.Places.Tests", "LouisAgent.Diagrams.Tests")]
    [TestCase("src/tests/evals", "LouisAgent.Evals.Tests", "LouisAgent.Evals.Checks.Tests",
        "LouisAgent.Evals.Protocol.Tests", "LouisAgent.Evals.Runner.Tests")]
    [TestCase("src/tests/shared", "LouisAgent.Testing")]
    [TestCase("src/tools", "LouisAgent.UsageProbe", "LouisAgent.FilmExport", "LouisAgent.DocDiagrams")]
    [TestCase("src/tests/tools", "LouisAgent.FilmExport.Tests", "LouisAgent.DocDiagrams.Tests")]
    [TestCase("src/tests/packages", "LouisAgent.Packages.IntegrationTests")]
    [TestCase("demos/louis-agent/tests", "LouisAgent.Coding.Catalog.Tests",
        "LouisAgent.CodeReview.Tests", "LouisAgent.Identities.Triage.Tests",
        "LouisAgent.Orchestrator.Tests", "LouisAgent.Orchestrator.IntegrationTests")]
    [TestCase("packs/tests", "LouisAgent.Packs.Web.Tests", "LouisAgent.Packs.Shell.Tests",
        "LouisAgent.Packs.Shell.IntegrationTests", "LouisAgent.Packs.Files.Tests", "LouisAgent.Packs.Git.Tests",
        "LouisAgent.Packs.Git.IntegrationTests", "LouisAgent.Packs.DotNet.Tests",
        "LouisAgent.Packs.DotNet.IntegrationTests", "LouisAgent.Packs.Calculator.Tests", "LouisAgent.Packs.Docs.Tests",
        "LouisAgent.Packs.Triage.Tests",
        "LouisAgent.Packs.Leads.Tests", "LouisAgent.Packs.Site.Tests",
        "LouisAgent.Packs.Files.Web.Tests", "LouisAgent.Packs.Git.Web.Tests", "LouisAgent.Packs.Rules.Tests")]
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

    /// <summary>
    /// The framework's own projects never live outside <c>src/</c>, so the old top-level folders are gone.
    /// </summary>
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

    /// <summary>A demo carries its own settings and images, so someone can read it as one whole project.</summary>
    [TestCase("demos/louis-agent", "config/.env.example", "config/prices.json", "config/rates.json",
        "docker/docker-compose.yml", "docker/images/Dockerfile.api", "docker/images/ollama/Dockerfile.ollama",
        "skills/README.md", "docs/SETUP.md", "docs/README.md")]
    public void ADemo_CarriesItsConfigAndImages(string demo, params string[] files)
    {
        var missing = files.Where(file => !File.Exists(Path.Combine(TestPaths.RepoRoot, demo, file)));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// The framework's site and the demo each keep their own copy, and until one has a reason to differ they can't
    /// drift apart by accident.
    /// </summary>
    [TestCase("prices.json")]
    [TestCase("rates.json")]
    public void TheSiteAndTheDemo_ShipTheSameFile(string file)
    {
        string site = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/config", file));
        string coding = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config", file));

        Assert.That(site.ReplaceLineEndings(), Is.EqualTo(coding.ReplaceLineEndings()));
    }

    /// <summary>
    /// What the solution lists is on disk, and no secret is listed: secrets only follow the folders. A demo's
    /// <c>.env</c> is listed but is yours, so a fresh checkout doesn't have it.
    /// </summary>
    [Test]
    public void ASolutionItem_IsAFileOnDisk()
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));

        // An item's line is its path, twice, inside a folder's section; a project's id never contains a slash.
        var items = lines.Select(line => line.Trim()).Where(line => line.Contains('\\') && line.Contains(" = "))
            .Where(line => !line.StartsWith("Project("))
            .Select(line => line.Split(" = ")[0].Replace('\\', '/')).ToList();
        string[] secrets = [".env.secrets", ".env.mongo", ".env.searxng"];
        Assert.That(items.Where(item => secrets.Contains(Path.GetFileName(item))), Is.Empty);

        var missing = items.Where(item => Path.GetFileName(item) != ".env")
            .Where(item => !File.Exists(Path.Combine(TestPaths.RepoRoot, item)));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// Every ADR on disk is in the solution, so Rider shows each decision beside the docs it belongs to. The check
    /// above only goes the other way, and 43 of them had gone missing without anyone noticing, Puls's whole folder
    /// among them.
    /// </summary>
    [Test]
    public void EveryAdr_IsASolutionItem()
    {
        string solution = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));

        // The framework's ADRs, the packs' and each demo's: every docs/adr folder there is.
        var folders = new[] { "src/docs/adr", "packs/docs/adr" }
            .Concat(TestPaths.Demos().Select(demo => Path.Combine(demo, "docs", "adr")))
            .Select(folder => Path.Combine(TestPaths.RepoRoot, folder))
            .Where(Directory.Exists);
        // The solution writes an item as its path, twice, with backslashes on every platform.
        var missing = folders.SelectMany(folder => Directory.EnumerateFiles(folder, "*.md"))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('/', '\\'))
            .Where(item => !solution.Contains($"{item} = {item}", StringComparison.Ordinal));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// Every file that instructs Claude is in the solution (CL63, Louis: "it should be attached to the solution"),
    /// so Rider's tree shows them as it shows the docs: the README of the instructions, the rules files and each
    /// skill. Only <c>src/docs/CLAUDE.md</c> used to be there.
    /// </summary>
    [Test]
    public void EveryInstructionFile_IsASolutionItem()
    {
        string solution = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));
        string claude = Path.Combine(TestPaths.RepoRoot, InstructionsFolder);
        string skillFolder = Path.Combine(TestPaths.RepoRoot, ".claude/skills");

        // What Claude Code reads under .claude, and the README that explains it.
        var files = Directory.EnumerateFiles(Path.Combine(claude, "rules"), "*.md")
            .Concat(Directory.Exists(skillFolder)
                ? Directory.EnumerateFiles(skillFolder, "*.md", SearchOption.AllDirectories)
                : [])
            .Concat(Directory.EnumerateFiles(claude, "README.md"));
        var missing = files
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('/', '\\'))
            .Where(item => !solution.Contains($"{item} = {item}", StringComparison.Ordinal));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// The folders at the top and the solution's groups at the top are the same list. Dot folders are not part of the
    /// code, what a run leaves behind sits in its demo, and a folder holding only build output is what a past move
    /// left. One dot folder is in the solution by name: <c>.claude</c>, which holds Claude's instructions (CL63,
    /// Louis's pick of three, after "it should be attached to the solution"), so Rider shows the rules and the skills
    /// where they are on disk.
    /// </summary>
    [Test]
    public void TheTopLevelFolders_AreTheSolutionsGroups()
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));

        var groups = lines.Where(line => line.StartsWith($"Project(\"{SolutionFolder}\")"))
            .Where(line => ParentOf(lines, IdOf(line)) is null)
            .Select(line => line.Split('"')[3]);
        var folders = Directory.EnumerateDirectories(TestPaths.RepoRoot)
            .Where(HoldsMoreThanBuildOutput)
            .Select(path => Path.GetFileName(path))
            .Where(name => !name.StartsWith('.') || name == InstructionsFolder);

        Assert.That(folders, Is.EquivalentTo(groups));
    }

    /// <summary>Whether a folder has anything in it besides what a build wrote.</summary>
    private static bool HoldsMoreThanBuildOutput(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/').Split('/'))
            .Any(parts => !parts.Contains("bin") && !parts.Contains("obj"));

    /// <summary>
    /// A file a project links from outside itself is really there. The build ignores a missing one, so a moved doc
    /// would otherwise leave a dead link behind without anyone noticing.
    /// </summary>
    [Test]
    public void AFileAProjectLinks_IsOnDisk()
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));
        var projects = lines.Where(line => line.StartsWith("Project(") && !line.Contains(SolutionFolder))
            .Select(line => Path.Combine(TestPaths.RepoRoot, line.Split('"')[5].Replace('\\', '/')));

        var missing = projects.SelectMany(project => LinkedFiles(project)
                .Where(file => !File.Exists(Path.Combine(Path.GetDirectoryName(project)!, file)))
                .Select(file => $"{Path.GetFileName(project)}: {file}"))
            .ToList();

        Assert.That(missing, Is.Empty);
    }

    /// <summary>What a project file's <c>None</c> items name outside its own folder, wildcards left out.</summary>
    private static IEnumerable<string> LinkedFiles(string project) =>
        File.ReadAllLines(project).Select(line => line.Trim())
            .Where(line => line.StartsWith("<None Include=\"..", StringComparison.Ordinal))
            .Select(line => line.Split('"')[1].Replace('\\', '/'))
            .Where(file => !file.Contains('*') && !file.Contains('$'));

    /// <summary>
    /// A project or a file sits at the path its groups spell, so the tree you read in Rider is the tree on disk.
    /// </summary>
    [Test]
    public void EverythingInTheSolution_SitsWhereItsGroupsSay()
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));
        var misplaced = new List<string>();

        string? group = null;
        foreach (string line in lines.Select(line => line.Trim()))
        {
            // A group's own line opens it, and every file line after it is that group's, until the next project line.
            if (line.StartsWith("Project(") && line.Contains(SolutionFolder)) group = IdOf(line);
            if (line.StartsWith("Project(") && !line.Contains(SolutionFolder))
            {
                group = null;
                string project = line.Split('"')[5].Replace('\\', '/');
                string where = GroupPath(lines, ParentOf(lines, IdOf(line))!);
                if (Path.GetDirectoryName(Path.GetDirectoryName(project))!.Replace('\\', '/') != where)
                    misplaced.Add($"{project} is in the group {where}");
            }

            if (group is null || line.StartsWith('{') || line.StartsWith("Project") || !line.Contains(" = ")) continue;
            string item = line.Split(" = ")[0].Replace('\\', '/');
            string folder = GroupPath(lines, group);
            if (Path.GetDirectoryName(item)!.Replace('\\', '/') != folder)
                misplaced.Add($"{item} is in the group {folder}");
        }

        Assert.That(misplaced, Is.Empty);
    }

    /// <summary>The path a solution folder's nesting spells, from the top.</summary>
    private static string GroupPath(string[] lines, string id)
    {
        string name = lines.Single(line => line.StartsWith("Project(") && IdOf(line) == id).Split('"')[3];
        return ParentOf(lines, id) is { } parent ? $"{GroupPath(lines, parent)}/{name}" : name;
    }

    /// <summary>The id at the end of a solution file's project line.</summary>
    private static string IdOf(string line) => line.Split('"')[^2];
}
