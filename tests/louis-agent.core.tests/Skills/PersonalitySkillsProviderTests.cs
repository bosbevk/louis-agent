using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Skills;

public class PersonalitySkillsProviderTests
{
    private static string SkillsDirectory => TestPaths.Prompts;

    [Test]
    public void Documentation_IsThePersonalityFile()
    {
        var provider = new PersonalitySkillsProvider(Path.Combine(SkillsDirectory, "agent", "louis-agent", "voice.md"));

        Assert.That(provider.Documentation, Does.Contain("Louis's Minion"));
        Assert.That(provider.Skills, Is.Empty, "personality.md is guidance only");
    }

    [Test]
    public void MissingFile_MeansNoPersonality()
    {
        var provider = new PersonalitySkillsProvider(null);

        Assert.That(provider.Documentation, Is.Empty);
    }

    [Test]
    public void AgentHostLoadSkills_PutsThePersonalityFirstAndOnce()
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };

        string docs = AgentHost.LoadSkills(options).Documentation;

        // CL9 step 2: the principles first, then the personality before everything else.
        Assert.That(docs, Does.StartWith("# Principles"));
        Assert.That(docs.IndexOf("# Louis's Minion - Personality", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal)));
        Assert.That(docs.Split("# Louis's Minion - Personality").Length - 1, Is.EqualTo(1));
        Assert.That(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal),
            Is.GreaterThan(0), "default.md follows the personality");
    }

    [Test]
    public void DefaultInstructions_NoLongerCarryThePersonality()
    {
        // CL9: the routing part of the coding agent's mode, after its voice.
        string defaults = Personalities.ModeParts(File.ReadAllText(Path.Combine(SkillsDirectory, "agent", "louis-agent", "voice.md"))).Routing;

        Assert.That(defaults, Does.Not.Contain("Minion").And.Not.Contain("Thiel"));
    }
}
