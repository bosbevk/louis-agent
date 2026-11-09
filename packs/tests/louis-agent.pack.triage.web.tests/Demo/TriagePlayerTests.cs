using System.Net;
using LouisAgent.Packs.Triage.Web.Demo;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Packs.Triage.Web.Tests.Demo;

/// <summary>
/// The triage demo plays a recorded run (F106.3.4): the stage lights the agent at work, a popup says what it did and
/// why, the steps gather in a list a visitor and the guide can read, and the run ends on the site's ending. The
/// visitor can step back and forth and play it again.
/// </summary>
public class TriagePlayerTests
{
    private const string Source = "recordings/triage-run.jsonl";

    private static string Recording => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "demos/louis-agent/web/louis-agent.code-review/wwwroot/recordings/triage-run.jsonl"));

    /// <summary>A context whose site serves the recorded run at <see cref="Source"/>, or nothing.</summary>
    private static BunitContext Context(bool served = true)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => served && request.RequestUri!.AbsolutePath.EndsWith(Source)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Recording) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        ctx.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://site.test/") });
        return ctx;
    }

    /// <summary>The site's ending: an offer the tests can find.</summary>
    private static readonly RenderFragment Offer =
        builder => builder.AddMarkupContent(0, "<p class=\"offer\">Yours?</p>");

    private static IRenderedComponent<TriagePlayer> Render(BunitContext ctx) =>
        ctx.Render<TriagePlayer>(parameters => parameters
            .Add(player => player.Source, Source)
            .Add(player => player.Pace, 0)
            .Add(player => player.Ending, Offer));

    /// <summary>Played through, every step is in the list, the last one is shown, and the ending follows.</summary>
    [Test]
    public void TheRun_PlaysThrough_ToItsEnding()
    {
        using var ctx = Context();

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".triage-steps li"), Has.Count.EqualTo(9));
            Assert.That(cut.Find(".step-popup").TextContent, Does.Contain("The case is closed"));
            Assert.That(cut.Find(".triage-stage .is-lit").GetAttribute("data-agent"), Is.EqualTo("code"));
        });
    }

    /// <summary>Back shows the step before, its agent lit and its popup open; the list stays whole.</summary>
    [Test]
    public void Back_ShowsTheStepBefore()
    {
        using var ctx = Context();
        var cut = Render(ctx);
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));

        cut.Find("button.triage-back").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".triage-stage .is-lit").GetAttribute("data-agent"), Is.EqualTo("reviewer"));
            Assert.That(cut.Find(".step-popup").TextContent, Does.Contain("reviewer"));
            Assert.That(cut.FindAll(".triage-steps li"), Has.Count.EqualTo(9));
        });
    }

    /// <summary>A recording the site can't serve goes straight to the ending, never a stage that waits.</summary>
    [Test]
    public void AMissingRun_GoesStraightToTheEnding()
    {
        using var ctx = Context(served: false);

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".triage-steps li"), Is.Empty);
    }
}
