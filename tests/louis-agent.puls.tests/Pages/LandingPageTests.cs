using LouisAgent.Puls.Pages;

namespace LouisAgent.Puls.Tests.Pages;

// T1b: Puls's landing page pinned to LandingPage.html before W2 splits it into sections. The page starts its motion
// through JS after the first render (pulsLanding.start) and stops it when left; the loose interop answers both with
// nothing. The demo's turns are example data serialised into an attribute: part of the markup, part of the golden.
public class LandingPageTests
{
    [Test]
    public void TheLandingPage_MatchesTheGoldenFile_AndStartsItsMotion()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<LandingPage>();

        Assert.That(cut.FindAll("section"), Has.Count.EqualTo(10), "the hero, the showcase, seven sections and the call to action");
        Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Elke vraag op pad na 'n antwoord"));
        Assert.That(ctx.JSInterop.Invocations.Select(i => i.Identifier), Is.EqualTo(new[] { "pulsLanding.start" }));
        Snapshots.Match("louis-agent.puls.tests", "Pages/LandingPage.html", cut.Markup);
    }
}
