using System.Net;
using System.Text;
using LouisAgent.Web.Components.Admin.Evals.LiveRun;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Admin.Evals.LiveRun;

/// <summary>
/// The strip on every admin page (F53.2 step 4): it says a run is going, names its eval and links to it; nothing
/// when no run is going; and it asks again while the run goes, so it leaves when the run ends.
/// </summary>
public class LiveRunStripTests
{
    /// <summary>The run in progress.</summary>
    private static readonly string Running = SampleEval.Fill(
        """
        {"id":"live1","check":"<EvalBeta>","repeat":2,"status":"running","exit_code":null,
         "started_at":"2026-10-27T10:00:00+00:00","ended_at":null,"lines":12,"stored":null,"store_error":null,
         "calibration_of":null,"grader":null,"last_line":null}
        """);

    /// <summary>The same run, ended.</summary>
    private static readonly string Done = Running.Replace("\"running\"", "\"done\"");

    /// <summary>
    /// The strip over an API whose current run is each of these answers in turn, the last one repeated.
    /// </summary>
    private static IRenderedComponent<LiveRunStrip> Render(BunitContext ctx, params string?[] answers)
    {
        int asked = 0;
        var handler = new StubHandler(request =>
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/runs/current")) return Status(HttpStatusCode.NotFound);
            string? answer = answers[Math.Min(asked++, answers.Length - 1)];
            return answer is null ? Status(HttpStatusCode.NotFound) : Json(answer);
        });
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        return ctx.Render<LiveRunStrip>(p => p.Add(x => x.Poll, TimeSpan.FromMilliseconds(20)));
    }

    /// <summary>A run that is going is named, with a link to its eval's page.</summary>
    [Test]
    public void ARunThatIsGoing_IsNamed_AndLinksToItsEval()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Running, Running);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".live-run-strip"), Has.Count.EqualTo(1)));
        var strip = cut.Find(".live-run-strip");
        Assert.That(strip.TextContent, Does.Contain(SampleEval.Beta).And.Contain("2 repeats"));
        Assert.That(strip.GetAttribute("href"), Is.EqualTo($"admin?eval={SampleEval.Beta}"));
    }

    /// <summary>No run going (the API has none yet): nothing on the page.</summary>
    [Test]
    public void NoRun_ShowsNothing()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, [null]);

        cut.WaitForAssertion(() => Assert.That(cut.Markup.Trim(), Is.Empty));
    }

    /// <summary>The strip asks again while the run goes, and leaves once it has ended.</summary>
    [Test]
    public void WhenTheRunEnds_TheStripLeaves()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Running, Running, Done);

        cut.WaitForState(() => cut.FindAll(".live-run-strip").Count == 1);
        // A predicate, not an assertion: NUnit keeps a failed assertion even when the wait catches it.
        cut.WaitForState(() => cut.FindAll(".live-run-strip").Count == 0, TimeSpan.FromSeconds(5));

        Assert.That(cut.FindAll(".live-run-strip"), Is.Empty);
    }

    /// <summary>A 200 with this JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>An answer with only a status and the API's error shape.</summary>
    private static HttpResponseMessage Status(HttpStatusCode status) =>
        new(status)
        {
            Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                Encoding.UTF8, "application/json")
        };
}
