namespace LouisAgent.Core.Tests;

/// <summary>
/// The folders on disk say what the solution says, and they say what ships. <c>src/</c> holds only the framework; a
/// pack is a tool someone takes, its checks sit apart so they can take it without them; and anything that names a
/// product is someone's own project under <c>demos/</c>.
/// </summary>
public class SolutionLayoutTests
{
    /// <summary>What marks a solution folder in the solution file, rather than a project.</summary>
    private const string SolutionFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>Each group holds the projects whose folder on disk has that name.</summary>
    [TestCase("src/engine", "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream",
        "louis-agent.composition", "louis-agent.core.web", "louis-agent.core.telemetry", "louis-agent.core.anthropic",
        "louis-agent.core.ollama", "louis-agent.core.openai")]
    [TestCase("src/hosts", "louis-agent.acp", "louis-agent.api", "louis-agent.api.contracts", "louis-agent.cli",
        "louis-agent.mcp", "louis-agent.accounts", "louis-agent.accounts.contracts", "louis-agent.api.evals")]
    [TestCase("src/web", "louis-agent.web", "louis-agent.web.accounts", "louis-agent.web.evals")]
    [TestCase("src/evals", "louis-agent.evals", "louis-agent.evals.checks", "louis-agent.evals.protocol",
        "louis-agent.evals.runner")]
    [TestCase("packs/tools", "louis-agent.pack.web", "louis-agent.pack.shell", "louis-agent.pack.files",
        "louis-agent.pack.git", "louis-agent.pack.dotnet", "louis-agent.pack.devops", "louis-agent.pack.paymo",
        "louis-agent.pack.recipes", "louis-agent.pack.calculator", "louis-agent.pack.shop-prices",
        "louis-agent.pack.where-to-buy", "louis-agent.pack.docs", "louis-agent.pack.triage", "louis-agent.pack.leads",
        "louis-agent.pack.site")]
    [TestCase("packs/evals", "louis-agent.pack.recipes.checks", "louis-agent.pack.where-to-buy.checks")]
    [TestCase("packs/web", "louis-agent.pack.recipes.web", "louis-agent.pack.files.web", "louis-agent.pack.git.web",
        "louis-agent.pack.triage.web")]
    [TestCase("demos/puls/identity", "louis-agent.identity.puls")]
    [TestCase("demos/puls/evals", "louis-agent.identity.puls.evals", "louis-agent.identity.puls.judges")]
    [TestCase("demos/louis-agent/evals", "louis-agent.identity.triage.evals")]
    [TestCase("demos/puls/web", "louis-agent.puls")]
    [TestCase("demos/puls/hosts", "louis-agent.host-packs", "louis-agent.puls-eval-runner", "louis-agent.puls-api")]
    [TestCase("demos/starter/web", "louis-agent.starter")]
    [TestCase("demos/starter/hosts", "louis-agent.starter-api")]
    [TestCase("demos/starter/tests", "louis-agent.starter.tests", "louis-agent.starter.integration-tests")]
    [TestCase("demos/louis-agent/identity", "louis-agent.identity.coding", "louis-agent.identity.triage")]
    [TestCase("src/site", "louis-agent.site", "louis-agent.identity.guide", "louis-agent.site-api",
        "louis-agent.identity.showroom")]
    [TestCase("src/tests/site", "louis-agent.site.tests")]
    [TestCase("demos/louis-agent/web", "louis-agent.code-review")]
    [TestCase("demos/louis-agent/hosts", "louis-agent.orchestrator", "louis-agent.coding-cli",
        "louis-agent.coding-acp", "louis-agent.coding-mcp", "louis-agent.coding-eval-runner",
        "louis-agent.coding-api", "louis-agent.coding-catalog")]
    [TestCase("src/tests/engine", "louis-agent.core.tests", "louis-agent.core.integration-tests",
        "louis-agent.choices.tests", "louis-agent.session-stream.tests", "louis-agent.core.web.tests",
        "louis-agent.core.telemetry.tests", "louis-agent.core.anthropic.tests", "louis-agent.core.ollama.tests",
        "louis-agent.core.openai.tests")]
    [TestCase("src/tests/hosts", "louis-agent.acp.tests", "louis-agent.mcp.tests",
        "louis-agent.accounts.tests", "louis-agent.accounts.integration-tests")]
    [TestCase("src/tests/web", "louis-agent.web.tests", "louis-agent.web.testing", "louis-agent.web.accounts.tests",
        "louis-agent.web.evals.tests")]
    [TestCase("src/tests/evals", "louis-agent.evals.tests", "louis-agent.evals.checks.tests",
        "louis-agent.evals.protocol.tests", "louis-agent.evals.runner.tests")]
    [TestCase("src/tests/shared", "louis-agent.testing")]
    [TestCase("src/tools", "louis-agent.usage-probe", "louis-agent.film-export")]
    [TestCase("src/tests/tools", "louis-agent.film-export.tests")]
    [TestCase("demos/puls/tests", "louis-agent.api.tests", "louis-agent.api.integration-tests",
        "louis-agent.api.evals.tests",
        "louis-agent.api.testing",
        "louis-agent.api.standin", "louis-agent.identity.puls.tests", "louis-agent.identity.puls.evals.tests",
        "louis-agent.identity.puls.judges.tests", "louis-agent.puls.tests", "louis-agent.host-packs.tests")]
    [TestCase("demos/louis-agent/tests", "louis-agent.coding-catalog.tests",
        "louis-agent.code-review.tests", "louis-agent.identity.triage.tests",
        "louis-agent.orchestrator.tests", "louis-agent.orchestrator.integration-tests")]
    [TestCase("packs/tests", "louis-agent.pack.web.tests", "louis-agent.pack.shell.tests",
        "louis-agent.pack.shell.integration-tests", "louis-agent.pack.files.tests", "louis-agent.pack.git.tests",
        "louis-agent.pack.git.integration-tests", "louis-agent.pack.dotnet.tests",
        "louis-agent.pack.dotnet.integration-tests", "louis-agent.pack.devops.tests", "louis-agent.pack.paymo.tests",
        "louis-agent.pack.recipes.tests", "louis-agent.pack.recipes.integration-tests",
        "louis-agent.pack.calculator.tests", "louis-agent.pack.docs.tests", "louis-agent.pack.triage.tests",
        "louis-agent.pack.leads.tests", "louis-agent.pack.site.tests",
        "louis-agent.pack.shop-prices.tests",
        "louis-agent.pack.shop-prices.integration-tests", "louis-agent.pack.where-to-buy.tests",
        "louis-agent.pack.recipes.checks.tests", "louis-agent.pack.where-to-buy.checks.tests",
        "louis-agent.pack.recipes.web.tests", "louis-agent.pack.files.web.tests", "louis-agent.pack.git.web.tests",
        "louis-agent.pack.triage.web.tests")]
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
    [TestCase("demos/puls", "config/.env.example", "config/prices.json", "config/rates.json",
        "docker/docker-compose.puls.yml", "docker/images/Dockerfile.puls", "docker/searxng/settings.yml",
        "skills/README.md", "deploy/README.md", "deploy/azure/deploy.ps1", "docs/SETUP.md", "docs/README.md")]
    [TestCase("demos/louis-agent", "config/.env.example", "config/prices.json", "config/rates.json",
        "docker/docker-compose.yml", "docker/images/Dockerfile.api", "docker/images/ollama/Dockerfile.ollama",
        "skills/README.md", "docs/SETUP.md", "docs/README.md")]
    public void ADemo_CarriesItsConfigAndImages(string demo, params string[] files)
    {
        var missing = files.Where(file => !File.Exists(Path.Combine(TestPaths.RepoRoot, demo, file)));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// Each demo keeps its own copy, and until one has a reason to differ they can't drift apart by accident.
    /// </summary>
    [TestCase("prices.json")]
    [TestCase("rates.json")]
    public void TheDemos_ShipTheSameFile(string file)
    {
        string puls = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/puls/config", file));
        string coding = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config", file));

        Assert.That(puls.ReplaceLineEndings(), Is.EqualTo(coding.ReplaceLineEndings()));
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
    /// The folders at the top and the solution's groups at the top are the same list. Dot folders are not part of the
    /// code, what a run leaves behind sits in its demo, and a folder holding only build output is what a past move
    /// left.
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
            .Where(name => !name.StartsWith('.'));

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
