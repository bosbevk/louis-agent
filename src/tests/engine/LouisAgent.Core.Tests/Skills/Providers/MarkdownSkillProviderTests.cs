using LouisAgent.Core.Skills.Providers;

namespace LouisAgent.Core.Tests.Skills.Providers;

/// <summary>One markdown file of the prompt: its text as it is, or none for a file that isn't there.</summary>
public class MarkdownSkillProviderTests
{
    /// <summary>
    /// A file is its text, nothing added (CL13, review of PR #59: who the agent is comes from the loader, which
    /// composes the files).
    /// </summary>
    [Test]
    public void AFile_IsItsText_NothingAdded()
    {
        string filePath = TestPaths.SampleSkills;

        Assert.That(new MarkdownSkillProvider(filePath).Documentation, Is.EqualTo(File.ReadAllText(filePath)));
    }

    /// <summary>A file that isn't there gives no text.</summary>
    [Test]
    public void AMissingFile_GivesNoText()
    {
        string missingFile = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.md");

        Assert.That(new MarkdownSkillProvider(missingFile).Documentation, Is.Empty);
    }

    /// <summary>A text already read is the provider's text, as it is.</summary>
    [Test]
    public void AText_IsItsDocumentation() =>
        Assert.That(MarkdownSkillProvider.FromText("# Voice\nSay it plainly.").Documentation,
            Is.EqualTo("# Voice\nSay it plainly."));
}
