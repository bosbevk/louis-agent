using System.Reflection;
using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.Identities.Expert;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Components.Guide;

/// <summary>
/// The guide knows the site it sits on (F109.4): its voice names every page, with the docs each was written from, so a
/// question about what a page says goes to its source and a new page can't be left out of the guide's map.
/// </summary>
public class GuideKnowsTheSiteTests
{
    /// <summary>The expert's voice, read the way the engine reads it: from the identity's embedded prompts.</summary>
    private static string TheVoice()
    {
        var identity = new ExpertIdentity();
        using var stream = identity.Prompts.GetFileInfo($"agent/{identity.DefaultVoice}.md").CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Every address of the site's pages is in the voice, as a backticked path.</summary>
    [Test]
    public void TheVoice_NamesEveryPageOfTheSite()
    {
        var addresses = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => $"`{route.Template}`").ToList();

        string voice = TheVoice();

        Assert.That(addresses, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
        Assert.That(addresses.Where(address => !voice.Contains(address)), Is.Empty);
    }

    /// <summary>
    /// The guide sells what the site sells, in the same words (FX72): the hosted site with an agent, at the one price
    /// the site publishes, so the corner and the page can't quote two different offers.
    /// </summary>
    [TestCase("What we sell today")]
    [TestCase("R500 to R1,000 a month")]
    [TestCase("closed Docker container")]
    public void TheVoice_SellsWhatTheSiteSells(string words)
    {
        Assert.That(TheVoice(), Does.Contain(words));
    }

    /// <summary>
    /// The guide answers from the docs, so the offer and Louis's address are in the doc the voice sends it to
    /// (Louis, 2026-11-08: "should be in the docs"), in the same words as the site.
    /// </summary>
    [TestCase("What the site sells, and how to reach Louis")]
    [TestCase("R500 to R1,000 a month")]
    [TestCase("louis.marais@live.com")]
    public void TheDocTheVoiceNames_HoldsTheOffer(string words)
    {
        string doc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/docs/projects/louis-agent.code-review.md"));

        Assert.That(TheVoice(), Does.Contain("louis-agent.code-review.md"));
        Assert.That(doc, Does.Contain(words));
    }
}
