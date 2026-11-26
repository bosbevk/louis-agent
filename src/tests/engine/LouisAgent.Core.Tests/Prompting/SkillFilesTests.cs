using System.Text.RegularExpressions;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Providers;

namespace LouisAgent.Core.Tests.Prompting;

/// <summary>
/// Our own skill files are plain markdown read by <c>MarkdownSkillProvider</c>, with no provider class each (core's
/// guide, *Adding a toolset*), and every path to one is named once, on <c>SkillFiles</c>. These check the pattern
/// holds, that the files say what their tools are, and where a skill the agent made lives.
/// </summary>
public class SkillFilesTests
{
    /// <summary>Core's own prompt files, as the assembly embeds them.</summary>
    private static string SkillsDirectory => TestPaths.Prompts;

    /// <summary>
    /// Every toolset has a guide, in <c>toolsets/</c>, that loads with it.
    /// </summary>
    [Test]
    public void EveryToolset_HasAGuide_ThatLoadsWithIt()
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
        }
    }

    /// <summary>
    /// F97.1: a skill the agent made is a folder named for it in the skills folder, and a draft waits in
    /// <c>drafts/</c>; beside its <c>SKILL.md</c> are its parameters and the example it was proved with.
    /// </summary>
    [Test]
    public void AnAgentsSkill_HasItsFolder_AndADraftWaitsInDrafts() =>
        Assert.That(
            (SkillFiles.ApprovedSkill("word-count"), SkillFiles.DraftSkill("word-count"), SkillFiles.ParametersFile,
                SkillFiles.ExampleFile),
            Is.EqualTo(("word-count", "drafts/word-count", "parameters.schema.json", "examples/example.json")));

    /// <summary>Only a skill format we don't control would need a provider class of its own.</summary>
    [Test]
    public void NoProviderClassPerSkillFile()
    {
        // Only skills we don't control would need a provider of their own; ours are all plain markdown.
        var providers = typeof(ISkillProvider).Assembly.GetTypes()
            .Where(t => typeof(ISkillProvider).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        // CL13: PersonalitySkillsProvider went, unused since F5 step 4b; the providers sit in Providers/, as every
        // seam's do.
        Assert.That(providers.Select(t => t.Name),
            Is.EquivalentTo(new[] { nameof(CompositeSkillProvider), nameof(MarkdownSkillProvider) }));
        Assert.That(providers.Select(t => t.Namespace), Has.All.EqualTo("LouisAgent.Core.Prompting.Providers"));
    }

    /// <summary>The host's prompt holds each toolset's guide once.</summary>
    [Test]
    public void AgentHostLoadSkills_IncludesEachToolsetGuideOnce()
    {
        var options = new AgentOptions { SkillsDirectory = SkillsDirectory };

        string docs = SkillLoader.Load(options, TestTools.Catalog.Toolsets).Documentation;

        Assert.That(Regex.Matches(docs, "# .NET Skills").Count, Is.EqualTo(1));
    }
}
