using System.Reflection;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Guide;
using LouisAgent.Site.Pages;
using Microsoft.AspNetCore.Components;

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

    /// <summary>
    /// Every page of the site is in the index he always has, so he can open the right one without reading the map
    /// first (F129's live check: with the whole map on demand he stopped opening the framework's pages).
    /// </summary>
    [Test]
    public void EveryPage_IsInTheIndexHeAlwaysHas()
    {
        var index = Read("skills/site-pages.md");
        var addresses = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => $"`{route.Template}`").ToList();

        Assert.That(index.Header?.Load, Is.EqualTo(SkillLoad.Always));
        Assert.That(addresses, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
        Assert.That(addresses.Where(address => !index.Body.Contains(address)), Is.Empty);
    }

    /// <summary>
    /// Running a demo live is a skill he always has (F127.4.2): it names every business he may ask with its demo's
    /// page, and how it goes: offered as cards, asked in the visitor's own words, answered in the business's window,
    /// and retold from its work.
    /// </summary>
    [Test]
    public void TheLiveDemo_IsASkillHeAlwaysHas_ForEveryBusinessHeMayAsk()
    {
        var skill = Read("skills/live-demo.md");
        string body = string.Join(' ', skill.Body.Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));

        Assert.Multiple(() =>
        {
            Assert.That(skill.Header?.Load, Is.EqualTo(SkillLoad.Always));
            Assert.That(new GuideIdentity().Calls.Where(address => !body.Contains($"`{address}`")), Is.Empty);
            Assert.That(body, Does.Contain("`Offer`").And.Contain("their own words").And.Contain("`AskAgent`")
                .And.Contain("its own window").And.Contain("`ReadAgentWork`"));
        });
    }

    /// <summary>
    /// FX133, from Louis's look at the live site: walking a demo page, *What to watch* ended with the walkthrough's
    /// three choices and never offered the live demo. The walkthrough now names that stop's own choices, and after the
    /// business answers he coaches: what it just did and why (Louis's pick, "run it live and coach").
    /// </summary>
    [Test]
    public void AtWhatToWatch_TheWalkthroughOffersTheDemoLive_AndHeCoachesAfterTheAnswer()
    {
        string voice = Flat(Read("voice.md").Body);
        string skill = Flat(Read("skills/live-demo.md").Body);

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain(
                    "On a demo page, the demo's own stop and the *What to watch* stop end with `Offer`: Ask it live"),
                "FX136, Louis at the demo's stop: \"still have not guided me\"");
            Assert.That(voice, Does.Contain("say what to watch for as it plays"));
            Assert.That(skill, Does.Contain("the walkthrough's *What to watch* stop"));
            Assert.That(skill, Does.Contain("what it just did and why"));
            Assert.That(skill, Does.Contain("End with `Offer`: two or three questions they could ask it next"),
                "Louis: \"when he is the teacher he must give me offers of what I could ask\"");
        });
    }

    /// <summary>A prompt's text on one line, so a rule can be found whatever its line breaks.</summary>
    private static string Flat(string text) =>
        string.Join(' ', text.Split((char[])[' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The voice keeps who he is and how he answers, and no longer carries either map.</summary>
    [Test]
    public void TheVoice_KeepsWhoHeIs_WithoutTheMaps() =>
        Assert.That(Read("voice.md").Body, Does.Contain("## How you answer").And.Contain("## What stays Louis's")
            .And.Not.Contain("## Where things are").And.Not.Contain("## The site you sit on"));
}
