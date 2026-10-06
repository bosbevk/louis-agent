using louis_agent.core;
using louis_agent.core.config;
using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class PersonalitySkillsProviderTests
{
    private static string SkillsDirectory => Path.Combine(TestPaths.RepoRoot, "Skills");

    [Test]
    public void Documentation_IsThePersonalityFile()
    {
        var provider = new PersonalitySkillsProvider(Path.Combine(SkillsDirectory, "personality.md"));

        Assert.That(provider.Documentation, Does.Contain("Louis's Minion"));
        Assert.That(provider.Skills, Is.Empty, "personality.md is guidance only");
    }

    [Test]
    public void MissingFile_MeansNoPersonality()
    {
        var provider = new PersonalitySkillsProvider(null);

        Assert.That(provider.Documentation, Is.Empty);
    }

    [TestCase("code-review")]
    [TestCase("louis")]
    public void AgentHostLoadSkills_PutsThePersonalityFirstAndOnce(string agentFunction)
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory, AgentFunction = agentFunction };

        string docs = AgentHost.LoadSkills(options).Documentation;

        Assert.That(docs, Does.StartWith("# Louis's Minion - Personality"));
        Assert.That(docs.Split("# Louis's Minion - Personality").Length - 1, Is.EqualTo(1));
        Assert.That(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal),
            Is.GreaterThan(0), "default.md follows the personality");
    }

    [Test]
    public void DefaultInstructions_NoLongerCarryThePersonality()
    {
        string defaults = File.ReadAllText(Path.Combine(SkillsDirectory, "default.md"));

        Assert.That(defaults, Does.Not.Contain("Minion").And.Not.Contain("Thiel"));
    }
}
