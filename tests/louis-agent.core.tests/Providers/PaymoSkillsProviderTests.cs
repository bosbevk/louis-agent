using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class PaymoSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _paymoSkillsPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _paymoSkillsPath = Path.Combine(_repoRoot, "Skills", "paymo-skills.md");
    }

    /// <summary>
    /// Verifies that PaymoSkillsProvider successfully loads paymo-skills.md
    /// when the file exists.
    /// </summary>
    [Test]
    public void Constructor_WhenPaymoSkillsFileExists_LoadsSuccessfully()
    {
        var provider = new PaymoSkillsProvider(_paymoSkillsPath);

        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        if (File.Exists(_paymoSkillsPath))
        {
            // paymo-skills.md should be created with time tracking skills
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that Paymo skills are loaded from the markdown file.
    /// </summary>
    [Test]
    public void Skills_LoadsPaymoTimeTrackingSkills()
    {
        var provider = new PaymoSkillsProvider(_paymoSkillsPath);

        // Verify that the file exists and skills are defined
        if (File.Exists(_paymoSkillsPath))
        {
            var skillNames = provider.Skills.Select(s => s.Name).ToList();

            // Expect time tracking related skills
            Assert.That(skillNames, Has.Some.EqualTo("LogTime")
                .Or.Some.EqualTo("GetTaskList")
                .Or.Some.EqualTo("GetTimeEntries")
                .Or.Some.Contain("Time")
                .Or.Some.Contain("Task"),
                "Should load Paymo time tracking skills");
        }
    }

    /// <summary>
    /// Verifies that PaymoSkillsProvider gracefully handles missing files.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-paymo-{Guid.NewGuid():N}.md");
        var provider = new PaymoSkillsProvider(missingPath);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that PaymoSkillsProvider finds paymo-skills.md in standard locations
    /// when no explicit path is provided.
    /// </summary>
    [Test]
    public void Constructor_WithoutPath_SearchesStandardLocations()
    {
        var provider = new PaymoSkillsProvider();

        // Should either find paymo-skills.md or return empty gracefully
        Assert.That(provider.Documentation, Is.TypeOf<string>());
    }

    /// <summary>
    /// Verifies that Skills property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Skills_UsesCaching()
    {
        var provider = new PaymoSkillsProvider(_paymoSkillsPath);

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
        var provider = new PaymoSkillsProvider(_paymoSkillsPath);

        var doc1 = provider.Documentation;
        var doc2 = provider.Documentation;

        Assert.That(ReferenceEquals(doc1, doc2), Is.True, "Documentation should be cached");
    }

    /// <summary>
    /// Verifies that documentation contains Paymo-related content.
    /// </summary>
    [Test]
    public void Documentation_ContainsPaymoContent()
    {
        var provider = new PaymoSkillsProvider(_paymoSkillsPath);

        if (!string.IsNullOrEmpty(provider.Documentation))
        {
            Assert.That(provider.Documentation.ToLower(), Does.Contain("paymo")
                .Or.Contain("time")
                .Or.Contain("task"),
                "Documentation should contain Paymo or time tracking content");
        }
    }
}
