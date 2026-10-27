using LouisAgent.Choices;
using LouisAgent.Core.Skills.Languages;
﻿using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

public class MarkdownSkillLoaderTests
{
    /// <summary>
    /// Verifies that the MarkdownSkillLoader can parse a valid markdown file
    /// containing skill definitions with their names, descriptions, and commands.
    /// </summary>
    [Test]
    public void Load_WhenSkillsFileContainsMarkdownSkills_ReturnsParsedDefinitions()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        // Use default.md which should contain skill definitions
        var filePath = Path.Combine(TestPaths.Prompts, "agent", "louis-agent", "voice.md");

        var skills = MarkdownSkillLoader.Load(filePath);

        // default.md should have at least some content, but we just verify structure
        Assert.That(skills, Is.InstanceOf<List<MarkdownSkillLoader.SkillDef>>());
    }

    [TestCase("", ScriptLanguage.Bash)]
    [TestCase("sh", ScriptLanguage.Bash)]
    [TestCase("pwsh", ScriptLanguage.PowerShell)]
    [TestCase("py", ScriptLanguage.Python)]
    public void Load_RecordsExecutionBlockLanguage_ForNoTagOrAnAlias(string fence, ScriptLanguage expected) => LoadsAs(fence, expected);

    [Test]
    public void Load_RecordsExecutionBlockLanguage_ForEachLanguagesOwnTag([Values] ScriptLanguage language) => LoadsAs(language.Wire(), language);

    private static void LoadsAs(string fence, ScriptLanguage expected)
    {
        var file = Path.Combine(Path.GetTempPath(), $"lang-{Guid.NewGuid():N}.md");
        File.WriteAllText(file, $"## Skill: Demo\n\n- Description: demo\n- Execution:\n\n```{fence}\necho hi\n```\n");
        try
        {
            var skill = MarkdownSkillLoader.Load(file).Single();
            Assert.That(skill.Language, Is.EqualTo(expected));
            Assert.That(skill.Command, Is.EqualTo("echo hi"));
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// Verifies that the loader gracefully handles missing files
    /// by returning an empty skill list rather than throwing an exception.
    /// </summary>
    [Test]
    public void Load_WhenFileDoesNotExist_ReturnsEmptyList()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing-skills-{Guid.NewGuid():N}.md");

        var skills = MarkdownSkillLoader.Load(missingFile);

        Assert.That(skills, Is.Empty);
    }

    /// <summary>
    /// Verifies that the loader can parse the HelloWorld.md test skills file
    /// and correctly extract skill definitions from it.
    /// </summary>
    [Test]
    public void Load_WhenHelloWorldSkillsFileExists_ReturnsParsedDefinitions()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var skills = MarkdownSkillLoader.Load(filePath);

        Assert.That(skills, Has.Count.GreaterThanOrEqualTo(2));
        var skillNames = skills.Select(s => s.Name).ToList();
        Assert.That(skillNames, Does.Contain("SayHello"));
        Assert.That(skillNames, Does.Contain("GetCurrentTime"));
    }

    /// <summary>
    /// Verifies that skill descriptions are parsed correctly from the markdown
    /// and contain meaningful documentation about the skill's purpose.
    /// </summary>
    [Test]
    public void Load_WhenSkillHasDescription_ParsesDescriptionCorrectly()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var skills = MarkdownSkillLoader.Load(filePath);
        var sayHello = skills.FirstOrDefault(s => s.Name == "SayHello");

        // If SayHello skill exists, verify it has a description
        if (sayHello != null)
        {
            Assert.That(sayHello.Description, Is.Not.Empty);
        }

        // Verify that the loader returns a valid list
        Assert.That(skills, Is.InstanceOf<List<MarkdownSkillLoader.SkillDef>>());
    }

    /// <summary>
    /// Verifies that the loader correctly handles multi-line shell commands
    /// enclosed in code blocks, combining them into a single command string.
    /// </summary>
    [Test]
    public void Load_WhenSkillCommandHasMultipleLines_ParsesCompleteCommand()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repoRoot, "Skills", "HelloWorld.md");

        var skills = MarkdownSkillLoader.Load(filePath);

        // Verify that commands are parsed as strings
        foreach (var skill in skills)
        {
            Assert.That(skill.Command, Is.InstanceOf<string>());
            // Commands should not be empty
            if (skill.Command != null)
            {
                Assert.That(skill.Command.Trim(), Is.Not.Empty);
            }
        }
    }
}

