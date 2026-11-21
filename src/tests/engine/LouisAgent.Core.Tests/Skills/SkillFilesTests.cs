using LouisAgent.Choices;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills;

// src/docs/CLAUDE.md, *Adding a toolset*: our own skill files are plain markdown read by MarkdownSkillProvider, with no
// provider class each. These check the pattern holds and that the files say what their tools are.
public class SkillFilesTests
{
    private static string SkillsDirectory => TestPaths.Prompts;

    private static MarkdownSkillProvider Load(string fileName) =>
        new(Path.Combine(SkillsDirectory, fileName));

    [Test]
    public void EveryToolset_HasAGuide_ThatLoadsWithIt_AndCantBeOverwritten()
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };
        foreach (var toolset in ToolsetCatalog.All)
        {
            Assert.That(toolset.GuideFile, Is.Not.Null, $"{toolset.Name} has a guide");
            string guide = toolset.GuideFile!;
            Assert.That(guide, Does.StartWith("toolsets/"),
                "CL8: every level shares the guides, so they're in toolsets/");
            Assert.That(File.Exists(Path.Combine(SkillsDirectory, guide)), Is.True, $"{guide} exists");
            string title = File.ReadLines(Path.Combine(SkillsDirectory, guide)).First(line => line.StartsWith("# "));
            Assert.That(SkillLoader.Load(options, [toolset]).Documentation, Does.Contain(title),
                $"{guide} loads with {toolset.Name}");
            Assert.That(SkillFiles.IsBuiltIn(guide), Is.True, $"{guide} can't be overwritten by the agent");
        }
    }

    [Test]
    public void TheFunctionSkills_AreLevelZeros() =>
        // CL13: written out in SkillFiles, so the top of Skills/ doesn't use Roles/; still the agent level's folder.
        Assert.That(SkillFiles.FunctionsFolder, Does.StartWith(RoleCatalog.AgentFolder + "/"));

    [Test]
    public void NoProviderClassPerSkillFile()
    {
        // Only skills we don't control would need a provider of their own; ours are all plain markdown.
        var providers = typeof(ISkillProvider).Assembly.GetTypes()
            .Where(t => typeof(ISkillProvider).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        // CL13: PersonalitySkillsProvider went, unused since F5 step 4b; the providers sit in Providers/, as every seam's do.
        Assert.That(providers.Select(t => t.Name),
            Is.EquivalentTo(new[] { nameof(CompositeSkillProvider), nameof(MarkdownSkillProvider) }));
        Assert.That(providers.Select(t => t.Namespace), Has.All.EqualTo("LouisAgent.Core.Skills.Providers"));
    }

    [Test]
    public void EveryExecuteSkillExample_NamesASkillThatExists()
    {
        // A guide that shows ExecuteSkill("X") for a procedure nobody defines sends the model to a failing call.
        var files = Directory.GetFiles(SkillsDirectory, "*.md");
        var defined = files
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"^## Skill:\s*(\S+)", RegexOptions.Multiline))
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

        string docs = SkillLoader.Load(options, TestTools.Catalog.Toolsets).Documentation;

        Assert.That(Regex.Matches(docs, "# .NET Skills").Count, Is.EqualTo(1));
    }

    [Test]
    public void TimeLogging_IsGone_ThePaymoToolsDoItsWork()
    {
        // CL8 step 6: its three skills only echoed a message; the paymo toolset logs real time.
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };

        Assert.That(SkillFileSource.ReadEmbedded(SkillFiles.Function("time-logging")), Is.Null);
        Assert.That(SkillLoader.Load(options).Skills.Select(s => s.Name),
            Has.None.AnyOf("LogWorkEntry", "GetWeeklySummary", "ValidateTimeEntry"));
    }
}
