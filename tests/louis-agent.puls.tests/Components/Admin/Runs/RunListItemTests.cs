using System.Net;
using System.Text;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Components.Admin.Runs;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Runs;

// F18 step 5b.2: a stored run's row in the portal. A finished run has a Re-score button that calls POST /v1/admin/benchmarks/runs/{id}/rescore; a re-scored run says
// which run it re-scored, with a link to it, and shows its cost as $0 (a known 0 from the API, never the dash that means unknown). Written first: the row was
// inline in the Benchmarks page, with neither.
public class RunListItemTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static RunSummary Run(string status = "done", TotalSummary? total = null, string? rescoreOf = null) =>
        new("abcdef123456", "stub", 1, status, At, total, null, null, rescoreOf);

    private static HttpResponseMessage Answer(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (IRenderedComponent<RunListItem> Cut, StubHandler Handler, List<string> Started) Render(
        RunSummary run, Func<HttpRequestMessage, HttpResponseMessage>? respond = null,
        bool disabled = false)
    {
        var ctx = new BunitContext();
        var handler = new StubHandler(respond ?? (_ =>
            Answer(HttpStatusCode.Created, """{"id":"new987654321","check":"stub"}""")));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));
        var started = new List<string>();
        var cut = ctx.Render<RunListItem>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.Disabled, disabled)
            .Add(x => x.OnRescored,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(started,
                    (Action<string>)(id => started.Add(id)))));
        return (cut, handler, started);
    }

    [Test]
    public void AFinishedRun_HasAReScoreButton_ThatCallsTheRoute_AndReportsTheNewRun()
    {
        var (cut, handler, started) = Render(Run());

        cut.Find("button.rescore").Click();

        cut.WaitForAssertion(() => Assert.That(started, Is.EqualTo(new[] { "new987654321" })));
        Assert.That(handler.Requests.Select(r => (r.Method, r.Url)),
            Is.EqualTo(new[] { (HttpMethod.Post, "http://localhost/v1/admin/benchmarks/runs/abcdef123456/rescore") }));
    }

    [Test]
    public void ARefusedReScore_SaysWhy_AndReportsNothing()
    {
        var (cut, _, started) = Render(Run(),
            _ => Answer(HttpStatusCode.Conflict,
                """{"type":"error","error":{"type":"conflict_error","message":"A run of stub is running."}}"""));

        cut.Find("button.rescore").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".rescore-error").TextContent, Does.Contain("A run of stub is running.")));
        Assert.That(started, Is.Empty);
    }

    [Test]
    public void ARunStillRunning_OrWhileAnotherDoes_HasNoEnabledButton()
    {
        Assert.That(Render(Run(status: "running")).Cut.FindAll("button.rescore"), Is.Empty, "nothing to re-score yet");
        Assert.That(Render(Run(), disabled: true).Cut.Find("button.rescore").HasAttribute("disabled"), Is.True,
            "one run at a time");
    }

    [Test]
    public void AReScoredRun_SaysWhichRunItReScored_WithALinkToIt_AndCostsDollarZero()
    {
        var (cut, _, _) = Render(Run(total: new TotalSummary(0m, 0), rescoreOf: "old123456789"));

        var note = cut.Find("a.rescored-from");
        Assert.That(note.TextContent, Is.EqualTo("re-scored from old12345"));
        Assert.That(note.GetAttribute("href"), Is.EqualTo("admin/benchmarks/old123456789"));
        Assert.That(cut.Find(".run-row-cost").TextContent, Is.EqualTo("$0"));
    }

    [Test]
    public void AnOrdinaryRun_HasNoReScoredNote_AndAnUnknownCostIsADash()
    {
        var (cut, _, _) = Render(Run(total: new TotalSummary(null, 3)));

        Assert.That(cut.FindAll(".rescored-from"), Is.Empty);
        Assert.That(cut.Find(".run-row-cost").TextContent, Is.EqualTo("—"));
        Assert.That(cut.Find("a.run-row-main").GetAttribute("href"), Is.EqualTo("admin/benchmarks/abcdef123456"));
    }
}
