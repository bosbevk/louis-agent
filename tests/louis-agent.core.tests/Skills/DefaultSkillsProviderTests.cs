using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

public class DefaultSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _defaultMdPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _defaultMdPath = Path.Combine(TestPaths.Prompts, "agent", "default.md");
    }

    /// <summary>
    /// Verifies that DefaultSkillsProvider successfully loads default.md
    /// when the file exists.
    /// </summary>
    [Test]
    public void Constructor_WhenDefaultMdExists_LoadsSuccessfully()
    {
        var provider = new DefaultSkillsProvider(_defaultMdPath);

        // default.md contains instructions, not skills, so might be empty
        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        Assert.That(provider.Documentation, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that DefaultSkillsProvider gracefully handles missing files.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-default-{Guid.NewGuid():N}.md");
        var provider = new DefaultSkillsProvider(missingPath);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that DefaultSkillsProvider finds default.md in standard locations
    /// when no explicit path is provided.
    /// </summary>
    [Test]
    public void Constructor_WithoutPath_SearchesStandardLocations()
    {
        var provider = new DefaultSkillsProvider();

        // Should either find default.md or return empty gracefully
        Assert.That(provider.Documentation, Is.TypeOf<string>());
    }

    /// <summary>
    /// Verifies that Skills property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Skills_UsesCaching()
    {
        var provider = new DefaultSkillsProvider(_defaultMdPath);

        var skills1 = provider.Skills;
        var skills2 = provider.Skills;

        Assert.That(ReferenceEquals(skills1, skills2), Is.True, "Skills should be cached");
    }

    /// <summary>
    /// Verifies that Documentation property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Documentation_UsesCaching()
    {
        var provider = new DefaultSkillsProvider(_defaultMdPath);

        var doc1 = provider.Documentation;
        var doc2 = provider.Documentation;

        Assert.That(ReferenceEquals(doc1, doc2), Is.True, "Documentation should be cached");
    }

    /// <summary>
    /// Verifies that documentation contains expected content from default.md.
    /// </summary>
    [Test]
    public void Documentation_ContainsAgentInstructions()
    {
        var provider = new DefaultSkillsProvider(_defaultMdPath);

        if (!string.IsNullOrEmpty(provider.Documentation))
        {
            // Expect default.md to contain tool documentation
            Assert.That(provider.Documentation.ToLower(), Does.Contain("workspace")
                .Or.Contain("git")
                .Or.Contain("tool"), "Documentation should contain agent instructions");
        }
    }
    
    /// <summary>
    /// default.md is a router: it names every toolset and describes no tools, so the prompt never shows a session
    /// tools it doesn't load (F5 step 4).
    /// </summary>
    [Test]
    public void Documentation_NamesEveryToolsetAndDescribesNoTools()
    {
        var provider = new DefaultSkillsProvider(_defaultMdPath);
        string doc = provider.Documentation;

        string[] toolsets = ["files", "git", "dotnet", "python", "powershell", "bash", "web", "skills", "paymo", "devops", "agents"];
        foreach (string toolset in toolsets)
            Assert.That(doc, Does.Contain($"`{toolset}`"), $"default.md should name the '{toolset}' toolset");

        string[] tools =
        [
            "ReadWorkspaceFile", "WriteWorkspaceFile", "GetStatus", "Commit(",
            "PythonRun", "WebSearch", "CreateSkillFile",
            "LogTimeByTaskName", "GetWorkItemsByAssignee",
        ];
        foreach (string tool in tools)
            Assert.That(doc, Does.Not.Contain(tool), $"default.md should not describe the {tool} tool");
    }
}
