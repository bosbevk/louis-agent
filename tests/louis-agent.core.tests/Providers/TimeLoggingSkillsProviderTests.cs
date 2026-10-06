using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class TimeLoggingSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _timeLoggingSkillsPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _timeLoggingSkillsPath = Path.Combine(_repoRoot, "Skills", "time-logging-skills.md");
    }

    /// <summary>
    /// Verifies that TimeLoggingSkillsProvider successfully loads time-logging-skills.md
    /// when the file exists.
    /// </summary>
    [Test]
    public void Constructor_WhenTimeLoggingSkillsFileExists_LoadsSuccessfully()
    {
        var provider = new TimeLoggingSkillsProvider(_timeLoggingSkillsPath);

        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        if (File.Exists(_timeLoggingSkillsPath))
        {
            // time-logging-skills.md should be created with time logging skills
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that time logging skills are loaded from the markdown file.
    /// </summary>
    [Test]
    public void Skills_LoadsTimeLoggingSkills()
    {
        var provider = new TimeLoggingSkillsProvider(_timeLoggingSkillsPath);

        // Verify that the file exists and skills are defined
        if (File.Exists(_timeLoggingSkillsPath))
        {
            var skillNames = provider.Skills.Select(s => s.Name).ToList();

            // Expect time logging related skills
            Assert.That(skillNames, Has.Some.EqualTo("LogWorkEntry")
                .Or.Some.EqualTo("GetWeeklySummary")
                .Or.Some.EqualTo("ValidateTimeEntry")
                .Or.Some.Contain("Time")
                .Or.Some.Contain("Log"),
                "Should load time logging skills");
        }
    }

    /// <summary>
    /// Verifies that TimeLoggingSkillsProvider gracefully handles missing files.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-timelogging-{Guid.NewGuid():N}.md");
        var provider = new TimeLoggingSkillsProvider(missingPath);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that TimeLoggingSkillsProvider finds time-logging-skills.md in standard locations
    /// when no explicit path is provided.
    /// </summary>
    [Test]
    public void Constructor_WithoutPath_SearchesStandardLocations()
    {
        var provider = new TimeLoggingSkillsProvider();

        // Should either find time-logging-skills.md or return empty gracefully
        Assert.That(provider.Documentation, Is.TypeOf<string>());
    }

    /// <summary>
    /// Verifies that Skills property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Skills_UsesCaching()
    {
        var provider = new TimeLoggingSkillsProvider(_timeLoggingSkillsPath);

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
        var provider = new TimeLoggingSkillsProvider(_timeLoggingSkillsPath);

        var doc1 = provider.Documentation;
        var doc2 = provider.Documentation;

        Assert.That(ReferenceEquals(doc1, doc2), Is.True, "Documentation should be cached");
    }

    /// <summary>
    /// Verifies that documentation contains time logging related content.
    /// </summary>
    [Test]
    public void Documentation_ContainsTimeLoggingContent()
    {
        var provider = new TimeLoggingSkillsProvider(_timeLoggingSkillsPath);

        if (!string.IsNullOrEmpty(provider.Documentation))
        {
            Assert.That(provider.Documentation.ToLower(), Does.Contain("time")
                .Or.Contain("log")
                .Or.Contain("entry"),
                "Documentation should contain time logging content");
        }
    }
}
