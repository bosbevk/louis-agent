using System.Net;
using System.Text;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Pages.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin;

/// <summary>
/// The run page as tabs (F53.1 step 2): the tab in the address decides which block is on the page, the summary when
/// the address names none, and moving between tabs doesn't fetch the run again.
/// </summary>
public class BenchmarkRunTabsTests
{
    /// <summary>A run with one conversation and no agreement rows.</summary>
    private static readonly string Run = SampleEval.Fill(SampleMode.Fill("""
        {"run":{"id":"run1","check":"<EvalBeta>","repeat":1,"status":"done","started_at":"2026-10-26T19:00:00+00:00","graders":[]},
         "modes":[{"name":"<ModeBeta>","repeat":1,"toolsets":["web"]}],"notes":[],"summaries":null,
         "conversations":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","role":null,"errors":0,"outcome":"Pass","grades":[],"human_grades":[]}],
         "turns":[],"requests":[],"agreement":[]}
        """));

    /// <summary>The list of checks, with this one's title.</summary>
    private const string Checks = """{"checks":[{"id":"puls-cook","title":"Puls cooks."}]}""";

    /// <summary>The services the page injects, over an API serving the run (reads counted) and the checks.</summary>
    private static (BunitContext Context, Func<int> RunReads) Open()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        int runReads = 0;
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.PathAndQuery;
            if (url.EndsWith("/runs/run1"))
            {
                runReads++;
                return Json(Run);
            }

            if (url.EndsWith("/v1/admin/benchmarks")) return Json(Checks);
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                    Encoding.UTF8, "application/json")
            };
        });
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());
        return (ctx, () => runReads);
    }

    /// <summary>The page at this address, drawn until its tab bar is there.</summary>
    private static IRenderedComponent<BenchmarkRun> Render(BunitContext ctx, string address)
    {
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        var cut = ctx.Render<BenchmarkRun>(p => p.Add(x => x.RunId, "run1"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".run-tabs"), Has.Count.EqualTo(1)));
        return cut;
    }

    /// <summary>With no tab in the address the page opens on the summary, and the explorer isn't on it.</summary>
    [Test]
    public void WithNoTab_ThePageOpensOnTheSummary()
    {
        var (ctx, _) = Open();
        using (ctx)
        {
            var cut = Render(ctx, "admin/runs/run1");

            Assert.That(cut.FindAll(".summary-none"), Has.Count.EqualTo(1), "the summary (here: none to show)");
            Assert.That(cut.FindAll(".explorer"), Is.Empty, "the explorer is on its own tab");
            Assert.That(cut.Find(".run-tabs a.active").TextContent, Does.Contain("Summary"));
        }
    }

    /// <summary>The notes tab shows the notes and nothing of the summary.</summary>
    [Test]
    public void TheNotesTab_ShowsTheNotes_AndNotTheSummary()
    {
        var (ctx, _) = Open();
        using (ctx)
        {
            var cut = Render(ctx, "admin/runs/run1?tab=notes");

            Assert.That(cut.FindAll(".notes-panel"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".summary-none"), Is.Empty);
            Assert.That(cut.Find(".run-tabs a.active").TextContent, Does.Contain("Notes"));
        }
    }

    /// <summary>A tab that doesn't exist opens the summary.</summary>
    [Test]
    public void ATabThatDoesntExist_OpensTheSummary()
    {
        var (ctx, _) = Open();
        using (ctx)
        {
            var cut = Render(ctx, "admin/runs/run1?tab=recipes");

            Assert.That(cut.FindAll(".summary-none"), Has.Count.EqualTo(1));
        }
    }

    /// <summary>
    /// Moving to another tab changes what is on the page without fetching the run again: the address changed, the
    /// run didn't.
    /// </summary>
    [Test]
    public void MovingToATab_DoesntFetchTheRunAgain()
    {
        var (ctx, runReads) = Open();
        using (ctx)
        {
            var cut = Render(ctx, "admin/runs/run1");
            Assert.That(runReads(), Is.EqualTo(1));

            ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/runs/run1?tab=conversations");

            cut.WaitForAssertion(() => Assert.That(cut.FindAll(".explorer"), Has.Count.EqualTo(1)));
            Assert.That(runReads(), Is.EqualTo(1), "the same run: nothing to fetch");
        }
    }

    /// <summary>
    /// A move in the explorer goes into the address (so Back goes up a level and a link opens the same view), and
    /// the run isn't fetched again for it.
    /// </summary>
    [Test]
    public void AMoveInTheExplorer_GoesIntoTheAddress_WithoutFetchingAgain()
    {
        var (ctx, runReads) = Open();
        using (ctx)
        {
            var cut = Render(ctx, "admin/runs/run1?tab=conversations");
            cut.WaitForAssertion(() => Assert.That(cut.FindAll(".tile"), Is.Not.Empty));

            cut.Find(".tile").Click();

            var navigation = ctx.Services.GetRequiredService<NavigationManager>();
            cut.WaitForAssertion(() =>
                Assert.That(navigation.Uri,
                    Does.EndWith("admin/runs/run1?tab=conversations&view=conversations")));
            cut.WaitForAssertion(() => Assert.That(cut.FindAll(".conv-filter"), Has.Count.EqualTo(1)));
            Assert.That(runReads(), Is.EqualTo(1), "the same run: nothing to fetch");
        }
    }

    /// <summary>A 200 with this JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
