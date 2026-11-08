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
}
