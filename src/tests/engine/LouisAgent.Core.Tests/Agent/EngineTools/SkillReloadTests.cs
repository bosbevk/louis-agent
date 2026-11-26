using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;

namespace LouisAgent.Core.Tests.Agent.EngineTools;

/// <summary>
/// F97.1 step 14d: nothing of the system prompt comes from the skills folder any more. A function skill file in the
/// old folder is never read, since a skill the agent keeps is a folder the door reads.
/// </summary>
public class SkillReloadTests
{
    private string _skillsDir = null!;

    /// <summary>Each test gets an empty skills folder of its own.</summary>
    [SetUp]
    public void SetUp() => _skillsDir = Directory.CreateTempSubdirectory("skills-").FullName;

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_skillsDir, recursive: true);

    /// <summary>
    /// A function skill file in the old folder no longer reaches the prompt, even with the skills toolset.
    /// </summary>
    [Test]
    public void AFunctionFile_IsNoLongerRead()
    {
        // The old place, written out: no type names it any more.
        string folder = Path.Combine(_skillsDir, PromptFolder.Agents.Wire(), "functions");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "greeting.md"), "# Greeting Skills\n\nGreets people.");
        var options = new AgentOptions { WorkspaceRoot = _skillsDir, SkillsDirectory = _skillsDir };

        Assert.That(SkillLoader.Load(options).Documentation, Does.Not.Contain("# Greeting Skills"));
    }
}
