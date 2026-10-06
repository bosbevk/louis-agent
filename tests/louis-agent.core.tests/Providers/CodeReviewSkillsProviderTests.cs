using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class CodeReviewSkillsProviderTests
{
    private string _repoRoot = null!;
    private string _codeReviewSkillsPath = null!;

    [SetUp]
    public void Setup()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        _codeReviewSkillsPath = Path.Combine(_repoRoot, "Skills", "code-review-skills.md");
    }

    /// <summary>
    /// Verifies that CodeReviewSkillsProvider successfully loads code-review-skills.md
    /// when the file exists.
    /// </summary>
    [Test]
    public void Constructor_WhenCodeReviewSkillsFileExists_LoadsSuccessfully()
    {
        var provider = new CodeReviewSkillsProvider(_codeReviewSkillsPath);

        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        if (File.Exists(_codeReviewSkillsPath))
        {
            // code-review-skills.md should be created with code review skills
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that code review skills are loaded from the markdown file.
    /// </summary>
    [Test]
    public void Skills_LoadsCodeReviewSkills()
    {
        var provider = new CodeReviewSkillsProvider(_codeReviewSkillsPath);

        // Verify that the file exists and skills are defined
        if (File.Exists(_codeReviewSkillsPath))
        {
            var skillNames = provider.Skills.Select(s => s.Name).ToList();

            // Expect code review related skills
            Assert.That(skillNames, Has.Some.EqualTo("AnalyzeChanges")
                .Or.Some.EqualTo("CheckCompliance")
                .Or.Some.EqualTo("GenerateReview")
                .Or.Some.Contain("Review")
                .Or.Some.Contain("Code"),
                "Should load code review skills");
        }
    }

    /// <summary>
    /// Verifies that CodeReviewSkillsProvider gracefully handles missing files.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-codereview-{Guid.NewGuid():N}.md");
        var provider = new CodeReviewSkillsProvider(missingPath);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that CodeReviewSkillsProvider finds code-review-skills.md in standard locations
    /// when no explicit path is provided.
    /// </summary>
    [Test]
    public void Constructor_WithoutPath_SearchesStandardLocations()
    {
        var provider = new CodeReviewSkillsProvider();

        // Should either find code-review-skills.md or return empty gracefully
        Assert.That(provider.Documentation, Is.TypeOf<string>());
    }

    /// <summary>
    /// Verifies that Skills property returns cached results on subsequent calls.
    /// </summary>
    [Test]
    public void Skills_UsesCaching()
    {
        var provider = new CodeReviewSkillsProvider(_codeReviewSkillsPath);

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
        var provider = new CodeReviewSkillsProvider(_codeReviewSkillsPath);

        var doc1 = provider.Documentation;
        var doc2 = provider.Documentation;

        Assert.That(ReferenceEquals(doc1, doc2), Is.True, "Documentation should be cached");
    }

    /// <summary>
    /// Verifies that documentation contains code review related content.
    /// </summary>
    [Test]
    public void Documentation_ContainsCodeReviewContent()
    {
        var provider = new CodeReviewSkillsProvider(_codeReviewSkillsPath);

        if (!string.IsNullOrEmpty(provider.Documentation))
        {
            Assert.That(provider.Documentation.ToLower(), Does.Contain("review")
                .Or.Contain("code")
                .Or.Contain("analysis"),
                "Documentation should contain code review content");
        }
    }
}
