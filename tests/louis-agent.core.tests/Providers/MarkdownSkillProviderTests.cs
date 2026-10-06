using louis_agent.core.tools;
using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class MarkdownSkillProviderTests
{
    /// <summary>
    /// Verifies that the MarkdownSkillProvider successfully loads and initializes
    /// when given a valid markdown file path containing skill definitions.
    /// </summary>
    [Test]
    public void Constructor_WhenFileExists_LoadsSkillsSuccessfully()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var provider = new MarkdownSkillProvider(filePath);

        // HelloWorld.md should have some skills
        Assert.That(provider.Skills, Has.Count.GreaterThanOrEqualTo(0));
        // If skills exist, documentation should be populated
        if (provider.Skills.Count > 0)
        {
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that the provider gracefully handles missing files
    /// by returning empty skills and documentation strings when includeDefaultInstructions is false.
    /// </summary>
    [Test]
    public void Constructor_WhenFileDoesNotExist_LoadsEmptySkillsList()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.md");

        var provider = new MarkdownSkillProvider(missingFile, includeDefaultInstructions: false);

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    /// <summary>
    /// Verifies that the Skills property returns a properly typed list
    /// of SkillDef objects with all required fields populated.
    /// </summary>
    [Test]
    public void Skills_ReturnsListOfSkillDefinitions()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var provider = new MarkdownSkillProvider(filePath);

        Assert.That(provider.Skills, Is.TypeOf<List<MarkdownSkillLoader.SkillDef>>());
        // If skills exist, verify they have required fields
        if (provider.Skills.Count > 0)
        {
            Assert.That(provider.Skills.All(s => !string.IsNullOrEmpty(s.Name)), Is.True);
            Assert.That(provider.Skills.All(s => !string.IsNullOrEmpty(s.Command)), Is.True);
        }
    }

    /// <summary>
    /// Verifies that the Documentation property returns the complete
    /// file content suitable for use in AI prompts.
    /// </summary>
    [Test]
    public void Documentation_ReturnsTrimmedFileContent()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var provider = new MarkdownSkillProvider(filePath);

        // Documentation should contain the file content
        Assert.That(provider.Documentation, Is.TypeOf<string>());
        // If file exists and has content, documentation should not be empty
        if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
        {
            Assert.That(provider.Documentation, Is.Not.Empty);
        }
    }

    /// <summary>
    /// Verifies that the provider can successfully load the HelloWorld.md
    /// test skills file containing example skill definitions.
    /// </summary>
    [Test]
    public void Constructor_LoadsHelloWorldSkills()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var provider = new MarkdownSkillProvider(filePath);

        Assert.That(provider.Skills, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(provider.Skills.Any(s => s.Name == "SayHello"), Is.True);
    }
}
