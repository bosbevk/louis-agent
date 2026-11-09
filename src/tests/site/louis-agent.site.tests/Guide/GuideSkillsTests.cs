using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// F129 (ADR 0114): who Louis is and how he answers stay in his voice, in every prompt; the map of the docs and the
/// site's pages are his skills, read on demand, so a message that needs neither doesn't carry them.
/// </summary>
public class GuideSkillsTests
{
    private const string Folder = "agent/louis-agent-guide/";

    /// <summary>A prompt file of the guide's, split into its header and the body the model reads.</summary>
    private static PromptFile Read(string path)
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts.GetFileInfo(Folder + path).CreateReadStream());
        return PromptHeaders.Split(Folder + path, reader.ReadToEnd());
    }

    /// <summary>Each map is a skill read on demand, under its own heading.</summary>
    [TestCase("docs-map", "# Where things are")]
    [TestCase("site-map", "# The site you sit on")]
    public void AMap_IsASkillReadOnDemand(string skill, string heading)
    {
        var file = Read($"skills/{skill}.md");

        Assert.That((file.Header?.Load, file.Body.StartsWith(heading, StringComparison.Ordinal)),
            Is.EqualTo(((SkillLoad?)SkillLoad.OnDemand, true)));
    }

    /// <summary>The voice keeps who he is and how he answers, and no longer carries either map.</summary>
    [Test]
    public void TheVoice_KeepsWhoHeIs_WithoutTheMaps() =>
        Assert.That(Read("voice.md").Body, Does.Contain("## How you answer").And.Contain("## What stays Louis's")
            .And.Not.Contain("## Where things are").And.Not.Contain("## The site you sit on"));
}
