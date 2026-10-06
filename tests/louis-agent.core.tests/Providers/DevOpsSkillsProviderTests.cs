using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class DevOpsSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _devopsSkillsPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _devopsSkillsPath = Path.Combine(_repoRoot, "Skills", "devops-skills.md");
    }

    /// <summary>
    /// Verifies that DevOpsSkillsProvider successfully loads devops-skills.md
    /// when the file exists.
    /// </summary>
    [Test]
    public void Constructor_WhenDevOpsSkillsFileExists_LoadsSuccessfully()
    {
        var provider = new DevOpsSkillsProvider(_devopsSkillsPath);

        // devops-skills.md should have skills defined
        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        if (File.Exists(_devopsSkillsPath))
        {
            Assert.That(provider.Skills.Count, Is.GreaterThan(0), "Should load DevOps skills");
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that specific DevOps skills are loaded from devops-skills.md.
    /// </summary>
    [Test]
    public void Skills_LoadsExpectedDevOpsSkills()
    {
        var provider = new DevOpsSkillsProvider(_devopsSkillsPath);

        if (provider.Skills.Count > 0)
        {
            var skillNames = provider.Skills.Select(s => s.Name).ToList();

            // Expect at least some of the documented skills
            Assert.That(skillNames, Has.Some.EqualTo("GetUserStories")
                .Or.Some.EqualTo("GetAllWorkItems")
                .Or.Some.EqualTo("GetWorkItemById"),
                "Should load one or more DevOps skills");
        }
    }

    /// <summary>
    /// Verifies that DevOpsSkillsProvider gracefully handles missing files.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-devops-{Guid.NewGuid():N}.md");
        var provider = new DevOpsSkillsProvider(missingPath);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that DevOpsSkillsProvider finds devops-skills.md in standard locations
    /// when no explicit path is provided.
    /// </summary>
    [Test]
    public void Constructor_WithoutPath_SearchesStandardLocations()
    {
        var provider = new DevOpsSkillsProvider();

        // Should either find devops-skills.md or return empty gracefully
        Assert.That(provider.Documentation, Is.TypeOf<string>());
    }

    /// <summary>
    /// Verifies that Skills property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Skills_UsesCaching()
    {
        var provider = new DevOpsSkillsProvider(_devopsSkillsPath);

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
        var provider = new DevOpsSkillsProvider(_devopsSkillsPath);

        var doc1 = provider.Documentation;
        var doc2 = provider.Documentation;

        Assert.That(ReferenceEquals(doc1, doc2), Is.True, "Documentation should be cached");
    }

    /// <summary>
    /// Verifies that all loaded skills have required properties.
    /// </summary>
    [Test]
    public void Skills_HaveAllRequiredProperties()
    {
        var provider = new DevOpsSkillsProvider(_devopsSkillsPath);

        foreach (var skill in provider.Skills)
        {
            Assert.That(skill.Name, Is.Not.Null.And.Not.Empty, "Skill name must be set");
            Assert.That(skill.Description, Is.Not.Null.And.Not.Empty, "Skill description must be set");
            Assert.That(skill.Command, Is.Not.Null.And.Not.Empty, "Skill command must be set");
        }
    }
}
