using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Pages.Admin;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin;

/// <summary>
/// Pins what the run page's head shows (the check's name and what it proves, the run's id, its status, start,
/// repeats, cost and requests, and the run it re-scored), so the page's rebuild keeps it (golden:
/// <c>BenchmarkRun.head.html</c>).
/// </summary>
public partial class BenchmarkRunHeadTests
{
    /// <summary>A finished re-score with a cost, and a store that kept only part of it.</summary>
    private static readonly string Run = SampleEval.Fill("""
                                                         {"run":{"id":"run2","check":"<EvalBeta>","repeat":3,"status":"done","started_at":"2026-10-26T09:00:00+00:00",
                                                                 "total":{"cost":1.25,"currency":"USD","tokens":1200,"requests":4},"store_error":"late","rescore_of":"run1"},
                                                          "modes":[],"conversations":[],"turns":[],"requests":[],"agreement":[]}
                                                         """);

    /// <summary>The list of checks, with this one's title.</summary>
    private static readonly string Checks =
        SampleEval.Fill("""{"checks":[{"id":"<EvalBeta>","title":"Puls cooks."}],"runs":[]}""");

    /// <summary>The head and the banner for a run the store didn't keep whole.</summary>
    [Test]
    public void TheHead_IsShownAsBefore()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Serve(ctx);

        var cut = ctx.Render<BenchmarkRun>(p => p.Add(x => x.RunId, "run2"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".store-banner"), Has.Count.EqualTo(1)));

        string head = cut.Find(".run-head").OuterHtml + cut.Find(".store-banner").OuterHtml;
        Snapshots.Match("louis-agent.puls.tests", "Pages/Admin/BenchmarkRun.head.html",
            LocalTime().Replace(head, "$1 HH:mm"));
    }

    /// <summary>The services the page injects, over an API that serves the run and the checks.</summary>
    private static void Serve(BunitContext ctx)
    {
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.PathAndQuery;
            if (url.EndsWith("/runs/run2")) return Json(Run);
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
    }

    /// <summary>A 200 with this JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>A date and a time of day as the page writes them, in the reader's own time zone.</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}")]
    private static partial Regex LocalTime();
}
