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
    private static string SkillsDirectory => TestPaths.Prompts;

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
            Assert.That(guide, Does.StartWith("toolsets/"), "CL8: every level shares the guides, so they're in toolsets/");
            Assert.That(File.Exists(Path.Combine(SkillsDirectory, guide)), Is.True, $"{guide} exists");
            string title = File.ReadLines(Path.Combine(SkillsDirectory, guide)).First();
            Assert.That(AgentHost.LoadSkills(options, [toolset]).Documentation, Does.Contain(title), $"{guide} loads with {toolset.Name}");
            Assert.That(SkillFiles.IsBuiltIn(guide), Is.True, $"{guide} can't be overwritten by the agent");
        }
    }

    /// <summary>
    /// The router names every toolset and describes no tools, so the prompt never shows a session tools it doesn't load
    /// (F5 step 4). Read from the assembly, as every session reads it (ADR 0018).
    /// </summary>
    [Test]
    public void TheRouter_NamesEveryToolsetAndDescribesNoTools()
    {
        // The coding agent's routing (CL9): Puls's routes only what Puls does.
        string doc = Personalities.ModeParts(SkillFileSource.ReadEmbedded(Personalities.FileName(Personalities.DefaultVoice, SkillLevel.Agent))!).Routing;

        // Every toolset but minions (F30): a specialist's toolset, never a chat's, and the coding product has no minion roles.
        foreach (string toolset in ToolsetCatalog.All.Select(t => t.Name).Where(name => name != "minions"))
            Assert.That(doc, Does.Contain($"`{toolset}`"), $"the router should name the '{toolset}' toolset");
        Assert.That(doc, Does.Not.Contain("`minions`"), "a chat never holds minions");

        string[] tools =
        [
            "ReadWorkspaceFile", "WriteWorkspaceFile", "GetStatus", "Commit(",
            "PythonRun", "WebSearch", "CreateSkillFile",
            "LogTimeByTaskName", "GetWorkItemsByAssignee",
        ];
        foreach (string tool in tools)
            Assert.That(doc, Does.Not.Contain(tool), $"the router should not describe the {tool} tool");
    }

    [Test]
    public void CodeReview_IsPartOfTheCodingAgentsMode()
    {
        // CL9 step 4: the placeholder code-review skills (they only echoed) go; how to review a change is in the mode.
        string routing = Personalities.ModeParts(SkillFileSource.ReadEmbedded(Personalities.FileName(Personalities.DefaultVoice, SkillLevel.Agent))!).Routing;

        Assert.That(SkillFileSource.ReadEmbedded(SkillFiles.Function("code-review")), Is.Null);
        Assert.That(routing, Does.Contain("Reviewing a change").And.Contain("`git`"));
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
            nameof(CompositeSkillProvider), nameof(MarkdownSkillProvider), nameof(PersonalitySkillsProvider),
        }));
    }

    [Test]
    public void DotNetGuide_NamesOnlyToolsThatExist()
    {
        // Guards against the guidance drifting from DotNetTools after a rename.
        var toolNames = typeof(DotNetTools).GetMethods().Select(m => m.Name).ToHashSet();
        var referenced = Regex.Matches(Load("toolsets/dotnet.md").Documentation, @"`(DotNet[A-Za-z]+)`")
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
        var referenced = Regex.Matches(Load("toolsets/paymo.md").Documentation, @"`([A-Z][A-Za-z]+)\(")
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

    [Test]
    public void AgentHostLoadSkills_IncludesEachToolsetGuideOnce()
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };

        string docs = AgentHost.LoadSkills(options).Documentation;

        Assert.That(Regex.Matches(docs, "# .NET Skills").Count, Is.EqualTo(1));
        Assert.That(Regex.Matches(docs, "# Paymo Skills").Count, Is.EqualTo(1));
    }

    [Test]
    public void TimeLogging_IsGone_ThePaymoToolsDoItsWork()
    {
        // CL8 step 6: its three skills only echoed a message; the paymo toolset logs real time.
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };

        Assert.That(SkillFileSource.ReadEmbedded(SkillFiles.Function("time-logging")), Is.Null);
        Assert.That(AgentHost.LoadSkills(options).Skills.Select(s => s.Name),
            Has.None.AnyOf("LogWorkEntry", "GetWeeklySummary", "ValidateTimeEntry"));
    }
}
