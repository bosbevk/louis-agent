using LouisAgent.Testing;
using System.Net;
using System.Text;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Pages.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin;

// F18 step 6a.4 (ADR 0042, Louis: "the web must always reflect the live results"): the run page renders what it last fetched, and after a grade it fetches the run again, so the chips, the
// review panel and the Judge-against-people card show what the store holds, not the copy the page opened with. Written first: the page fetched the run once.
public class BenchmarkRunTests
{
    private static readonly string Run = SampleEval.Fill(SampleMode.Fill(SampleRole.Fill("""
        {"run":{"id":"run1","check":"<EvalBeta>","repeat":1,"status":"done","started_at":"2026-10-26T19:00:00+00:00",
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"meal-for-people","version":1,"kind":"Model"},{"name":"meal-for-people.human","version":2,"kind":"Human","question":"Is this a meal?"}]},
         "modes":[{"name":"<ModeBeta>","repeat":1,"toolsets":["web"]}],
         "conversations":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","role":null,"scores":{},"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds four"},
                      {"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],"human_grades":[]}],
         "turns":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kook ek?","answer":"Bobotie.","role":"<RoleAlpha>","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
         "requests":[],
         "agreement":[{"grader":"meal-for-people","person_pass":{"judge_pass":0,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]}
        """)));

    private const string Checks = """{"checks":[{"id":"puls-cook","title":"Puls cooks."}]}""";

    [Test]
    public void AfterAGrade_ThePageFetchesTheRunAgain_AndKeepsTheOpenTask()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        int runReads = 0;
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.PathAndQuery;
            if (url.EndsWith("/runs/run1")) { runReads++; return Json(Run); }
            if (url.EndsWith("/v1/admin/benchmarks")) return Json(Checks);
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""", Encoding.UTF8, "application/json") };
        });
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/benchmarks/run1?task=bobotie");
        var cut = ctx.Render<BenchmarkRun>(p => p.Add(x => x.RunId, "run1"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".review-panel"), Has.Count.EqualTo(1)));
        Assert.That(runReads, Is.EqualTo(1));

        state.Record(new ConversationRef("run1", SampleMode.Beta, 1, "bobotie"), "meal-for-people");

        cut.WaitForAssertion(() => Assert.That(runReads, Is.EqualTo(2), "the run is fetched again after a grade"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".review-panel"), Has.Count.EqualTo(1), "and the open conversation stays open"));
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
