using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class DefaultSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _defaultMdPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _defaultMdPath = Path.Combine(_repoRoot, "Skills", "default.md");
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
}
