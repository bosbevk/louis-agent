using System.Text.RegularExpressions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.DotNet;
using LouisAgent.Core.Tools.Integrations.Paymo;

namespace LouisAgent.Core.Tests.Skills;

// docs/CLAUDE.md, *Adding a toolset*: our own skill files are plain markdown read by MarkdownSkillProvider, with no
// provider class each. These check the pattern holds and that the files say what their tools are.
public class SkillFilesTests
{
    private static string SkillsDirectory => Path.Combine(TestPaths.RepoRoot, "Skills");

    private static MarkdownSkillProvider Load(string fileName) =>
        new(Path.Combine(SkillsDirectory, fileName), includeDefaultInstructions: false);

    [Test]
    public void EveryToolset_HasAGuide_ThatLoadsWithIt_AndCantBeOverwritten()
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };
        foreach (var toolset in ToolsetCatalog.All)
        {
            Assert.That(toolset.GuideFile, Is.Not.Null, $"{toolset.Name} has a guide");
            string guide = toolset.GuideFile!;
            Assert.That(File.Exists(Path.Combine(SkillsDirectory, guide)), Is.True, $"{guide} exists");
            string title = File.ReadLines(Path.Combine(SkillsDirectory, guide)).First();
            Assert.That(AgentHost.LoadSkills(options, [toolset]).Documentation, Does.Contain(title), $"{guide} loads with {toolset.Name}");
            Assert.That(SkillFiles.BuiltIn, Does.Contain(guide), $"{guide} can't be overwritten by the agent");
        }
    }

    [Test]
    public void NoProviderClassPerSkillFile()
    {
        // Only skills we don't control would need a provider of their own; ours are all plain markdown.
        var providers = typeof(ISkillProvider).Assembly.GetTypes()
            .Where(t => typeof(ISkillProvider).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name);

        Assert.That(providers, Is.EquivalentTo(new[]
        {
            nameof(CompositeSkillProvider), nameof(MarkdownSkillProvider), nameof(DefaultSkillsProvider), nameof(PersonalitySkillsProvider),
        }));
    }

    [Test]
    public void DotNetGuide_NamesOnlyToolsThatExist()
    {
        // Guards against the guidance drifting from DotNetTools after a rename.
        var toolNames = typeof(DotNetTools).GetMethods().Select(m => m.Name).ToHashSet();
        var referenced = Regex.Matches(Load("dotnet-skills.md").Documentation, @"`(DotNet[A-Za-z]+)`")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.That(referenced, Does.Contain("DotNetBuild").And.Contain("DotNetTest"));
        Assert.That(referenced.Where(name => !toolNames.Contains(name)), Is.Empty);
    }

    [Test]
    public void PaymoGuide_NamesOnlyToolsThatExist()
    {
        var toolNames = typeof(PaymoTools).GetMethods().Select(m => m.Name).ToHashSet();
        var referenced = Regex.Matches(Load("paymo-skills.md").Documentation, @"`([A-Z][A-Za-z]+)\(")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.That(referenced, Does.Contain("LogTimeByTaskName"));
        Assert.That(referenced.Where(name => !toolNames.Contains(name)), Is.Empty);
    }

    [Test]
    public void EveryExecuteSkillExample_NamesASkillThatExists()
    {
        // A guide that shows ExecuteSkill("X") for a procedure nobody defines sends the model to a failing call.
        var files = Directory.GetFiles(SkillsDirectory, "*.md");
        var defined = files.SelectMany(f => Regex.Matches(File.ReadAllText(f), @"^## Skill:\s*(\S+)", RegexOptions.Multiline))
            .Select(m => m.Groups[1].Value)
            .ToHashSet();
        var dangling = files
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"ExecuteSkill\(\s*""([^""]+)""")
                .Select(m => (File: Path.GetFileName(f), Skill: m.Groups[1].Value)))
            .Where(reference => !defined.Contains(reference.Skill))
            .Select(reference => $"{reference.File}: {reference.Skill}");

        Assert.That(dangling, Is.Empty);
    }

    [TestCase("code-review-skills.md", "AnalyzeChanges")]
    [TestCase("time-logging-skills.md", "LogWorkEntry")]
    public void FunctionSkillFiles_HaveTheirProcedures(string fileName, string skill)
    {
        Assert.That(Load(fileName).Skills.Select(s => s.Name), Does.Contain(skill));
    }

    [TestCase("code-review")]
    [TestCase("louis")]
    [TestCase("dotnet")]
    [TestCase("paymo")]
    public void AgentHostLoadSkills_IncludesEachToolsetGuideOnce(string agentFunction)
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory, AgentFunction = agentFunction };

        string docs = AgentHost.LoadSkills(options).Documentation;

        Assert.That(Regex.Matches(docs, "# .NET Skills").Count, Is.EqualTo(1));
        Assert.That(Regex.Matches(docs, "# Paymo Skills").Count, Is.EqualTo(1));
    }

    [TestCase("code-review", "AnalyzeChanges")]
    [TestCase("time-logging", "LogWorkEntry")]
    public void AgentHostLoadSkills_LoadsTheFunctionsOwnFile(string agentFunction, string skill)
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory, AgentFunction = agentFunction };

        Assert.That(AgentHost.LoadSkills(options).Skills.Select(s => s.Name), Does.Contain(skill));
    }
}
