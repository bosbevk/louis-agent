using System.Text.RegularExpressions;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;

namespace louis_agent.core.tests;

public class DotNetSkillsProviderTests
{
    private static string SkillsPath => Path.Combine(TestPaths.RepoRoot, "Skills", "dotnet-skills.md");

    [Test]
    public void Documentation_LoadsDotNetGuidance()
    {
        var provider = new DotNetSkillsProvider(SkillsPath);

        Assert.That(provider.Documentation, Does.Contain("DotNetBuild"));
        Assert.That(provider.Documentation, Does.Contain("DotNetTest"));
    }

    [Test]
    public void Documentation_OnlyReferencesToolsThatExist()
    {
        // Guards against the guidance drifting from DotNetTools after a rename.
        var toolNames = typeof(DotNetTools).GetMethods().Select(m => m.Name).ToHashSet();
        var referenced = Regex.Matches(new DotNetSkillsProvider(SkillsPath).Documentation, @"`(DotNet[A-Za-z]+)`")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.That(referenced, Is.Not.Empty);
        Assert.That(referenced.Where(name => !toolNames.Contains(name)), Is.Empty);
    }

    [Test]
    public void Constructor_WhenFileDoesNotExist_ReturnsEmptyCollections()
    {
        var provider = new DotNetSkillsProvider(Path.Combine(Path.GetTempPath(), $"missing-dotnet-{Guid.NewGuid():N}.md"));

        Assert.That(provider.Skills, Is.Empty);
        Assert.That(provider.Documentation, Is.Empty);
    }

    [Test]
    public void Documentation_UsesCaching()
    {
        var provider = new DotNetSkillsProvider(SkillsPath);
        Assert.That(ReferenceEquals(provider.Documentation, provider.Documentation), Is.True);
    }

    [TestCase("code-review")]
    [TestCase("louis")]
    [TestCase("dotnet")]
    public void AgentHostLoadSkills_IncludesDotNetGuidanceOnce(string agentFunction)
    {
        var options = new AgentOptions { SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills"), AgentFunction = agentFunction };

        string docs = AgentHost.LoadSkills(options).Documentation;

        Assert.That(Regex.Matches(docs, "# .NET Skills").Count, Is.EqualTo(1));
    }
}
