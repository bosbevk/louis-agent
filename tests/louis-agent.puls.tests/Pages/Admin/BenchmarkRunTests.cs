using LouisAgent.Testing;
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
/// The run page shows what it last fetched, and after a grade or a note it fetches the run again, so the chips, the review panel, the notes and the judge-against-people card show what the store holds, not the copy the page opened with.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public class BenchmarkRunTests
{
    /// <summary>A run with one conversation and a human review.</summary>
    private static readonly string Run = SampleEval.Fill(SampleMode.Fill(SampleRole.Fill("""
        {"run":{"id":"run1","check":"<EvalBeta>","repeat":1,"status":"done","started_at":"2026-10-26T19:00:00+00:00",
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"meal-for-people","version":1,"kind":"Model"},{"name":"meal-for-people.human","version":2,"kind":"Human","question":"Is this a meal?"}]},
         "modes":[{"name":"<ModeBeta>","repeat":1,"toolsets":["web"]}],
         "conversations":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","role":null,"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds four"},
                      {"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],"human_grades":[]}],
         "turns":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kook ek?","answer":"Bobotie.","role":"<RoleAlpha>","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
         "requests":[],
         "agreement":[{"grader":"meal-for-people","person_pass":{"judge_pass":0,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]}
        """)));

    /// <summary>The list of checks, with this one's title.</summary>
    private const string Checks = """{"checks":[{"id":"puls-cook","title":"Puls cooks."}]}""";

    /// <summary>After a grade the page fetches the run again, and the conversation you had open stays open.</summary>
    [Test]
    public void AfterAGrade_ThePageFetchesTheRunAgain_AndKeepsTheOpenTask()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // An API that serves the run (counting how often it is read) and the checks, and says "no" to anything else.
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

        // The services the page injects.
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        ctx.Services.AddSingleton(new RunNotesState());

        // Open the page with a conversation to focus, and wait until it has drawn the review panel, having read the run
        // once.
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/benchmarks/run1?task=bobotie");
        var cut = ctx.Render<BenchmarkRun>(p => p.Add(x => x.RunId, "run1"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".review-panel"), Has.Count.EqualTo(1)));
        Assert.That(runReads, Is.EqualTo(1));

        // The reviewer grades it.
        state.Record(new ConversationRef("run1", SampleMode.Beta, 1, "bobotie"), "meal-for-people");

        cut.WaitForAssertion(() => Assert.That(runReads, Is.EqualTo(2), "the run is fetched again after a grade"));
        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll(".review-panel"), Has.Count.EqualTo(1), "and the open conversation stays open"));
    }

    /// <summary>
    /// After a note is written the page fetches the run again and shows the notes the store holds, not a copy it made
    /// itself.
    /// </summary>
    [Test]
    public void AfterANote_ThePageFetchesTheRunAgain_AndShowsTheNotesTheStoreHolds()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // An API whose second read of the run carries a note, as the store would after someone wrote one.
        int runReads = 0;
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.PathAndQuery;
            if (url.EndsWith("/runs/run1"))
            {
                runReads++;
                return Json(runReads == 1
                    ? Run
                    : Run.Replace("\"requests\":[],",
                        "\"requests\":[],\"notes\":[{\"text\":\"judge changed\",\"at\":\"2026-10-27T09:00:00+00:00\",\"conversation\":null}],"));
            }

            if (url.EndsWith("/v1/admin/benchmarks")) return Json(Checks);
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                    Encoding.UTF8, "application/json")
            };
        });

        // The services the page injects.
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        var notes = new RunNotesState();
        ctx.Services.AddSingleton(notes);

        // The first read has no notes, so the panel says so.
        var cut = ctx.Render<BenchmarkRun>(p => p.Add(x => x.RunId, "run1"));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".notes-empty"), Has.Count.EqualTo(1)));
        Assert.That(runReads, Is.EqualTo(1));

        // Someone writes a note: the form tells the state.
        notes.Written();

        cut.WaitForAssertion(() => Assert.That(runReads, Is.EqualTo(2), "the run is fetched again after a note"));
        cut.WaitForAssertion(() => Assert.That(cut.Find(".note-body").TextContent, Is.EqualTo("judge changed")));
    }

    /// <summary>A 200 answer with this JSON.</summary>
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
}
