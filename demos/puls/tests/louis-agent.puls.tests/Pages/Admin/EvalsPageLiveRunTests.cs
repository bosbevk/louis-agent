using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Testing;
using LouisAgent.Web.Usage;
using EvalsPage = LouisAgent.Puls.Pages.Admin.Evals;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin;

/// <summary>
/// The Evals page runs an eval from its details (F53.2 step 3, what the Benchmarks page did): a run going before the
/// page opened is picked up and followed to its end, then the list is read again; the eval's manual opens from the
/// details; the runs table names the eval (goldens: <c>Evals.live.html</c>, <c>Evals.manual.html</c>).
/// </summary>
public partial class EvalsPageLiveRunTests
{
    /// <summary>Two evals, the first with a manual and one run; the file beside the page's other tests.</summary>
    private static readonly string List = Fill(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/puls/tests/louis-agent.puls.tests", "Pages", "Admin", "Evals.two.json")));

    /// <summary>The run in progress when the page opens: the first eval's.</summary>
    private static readonly string Current = Fill(
        """
        {"id":"live1","check":"<EvalBeta>","repeat":1,"status":"running","exit_code":null,
         "started_at":"2026-10-27T10:00:00+00:00","ended_at":null,"lines":0,"stored":null,"store_error":null,
         "calibration_of":null,"grader":null,"last_line":null}
        """);

    /// <summary>
    /// What the store holds of the live run: its first turn, its conversation, and the requests' costs.
    /// </summary>
    private static readonly string Stored = Fill(
        """
        {"run":{"id":"live1","check":"<EvalBeta>","repeat":1,"status":"running",
                "started_at":"2026-10-27T10:00:00+00:00"},
         "modes":[],"notes":[],"agreement":[],
         "turns":[{"tools":{"calls":0,"failed":0,"reasons":[]},"mode":"<ModeAlpha>","repeat":1,"task":"bobotie",
                   "turn":1,"session":"s1","question":"Maak vir my bobotie.","answer":"Hier is bobotie.",
                   "role":"<RoleAlpha>","continued":null,"agent_calls":1,"specialist_calls":2,"hosts":[],
                   "seconds":3.5,"error":null}],
         "conversations":[{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","order":0,"role":"<RoleAlpha>",
                           "errors":1,"seconds":7.5,"human_grades":[]}],
         "requests":[
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:00:01+00:00",
           "purpose":"turn","cost":0.01,"duration_ms":10},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:00:02+00:00",
           "purpose":"turn","cost":0.02,"duration_ms":10},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":2},"at":"2026-10-27T10:00:03+00:00",
           "purpose":"turn","cost":0.03,"duration_ms":10},
          {"turn":null,"at":"2026-10-27T10:00:04+00:00","purpose":"judge","cost":0.04,"duration_ms":10}]}
        """);

    /// <summary>When the live run's events happened; the page doesn't read it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The live run's events, written by the contract's own writer: a plain line, a question that is never answered,
    /// a turn that streams and ends, the conversation's end, a broken line, and the end.
    /// </summary>
    private static readonly string Events = string.Join("\n\n",
    [
        "data: starting the run",
        Data(new QuestionEvent(T0, SampleMode.Alpha, 1, "chat", 1, "s0", "Gee my drie name vir 'n hond.")),
        Data(new QuestionEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "s1",
            "En as ek dit vir ses mense wil maak, met genoeg rys en blatjang vir almal aan die tafel?")),
        Data(new SseEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "content_block_start", Parsed(
            """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""))),
        Data(new SseEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "content_block_delta", Parsed(
            """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Verdubbel alles."}}"""))),
        Data(new TurnEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "s1", "En vir ses?", "Verdubbel alles.",
            SampleRole.Alpha, "sub-1", 1, 3, [], 4.25, null, null)),
        Data(new ConversationEvent(T0, SampleMode.Alpha, 1, "bobotie", SampleRole.Alpha, 0, 7.75, [],
            TrialOutcome.Fail, new Metrics(2, 5, [], 2, null, null, 7.75, null))),
        "data: { not json",
        "event: done\ndata: " + JsonSerializer.Serialize(
            new LiveRunRow("live1", SampleEval.Beta, 1, "done", 0, T0, null, 9, true, null, null, null, null),
            AdminJson.SnakeCase),
    ]) + "\n\n\n";

    /// <summary>The first eval's manual.</summary>
    private static readonly string Manual =
        Fill("""{"check":"<EvalBeta>","title":"How Puls cooks","markdown":"# Cooks\n\nIt **cooks**."}""");

    /// <summary>One of the runner's events as the stream's data line, written by the contract's writer.</summary>
    private static string Data(EvalEvent e) => "data: " + EvalProtocolJson.Line(e);

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Parsed(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>
    /// The page picks up the run that was going, follows it to its end in the details column, and reads the list
    /// again; the run's eval is the open one.
    /// </summary>
    [Test]
    public void ARunThatWasGoing_IsFollowedToItsEnd_AndTheListReadAgain()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var served = Serve(ctx);

        var cut = ctx.Render<EvalsPage>();
        cut.WaitForAssertion(() =>
        {
            Assert.That(served.Lists, Is.EqualTo(2));
            Assert.That(cut.FindAll(".eval-pane .run-panel.done"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".page-actions button").HasAttribute("disabled"), Is.False);
        }, TimeSpan.FromSeconds(10));

        Assert.That(cut.Find(".eval-details .eval-title code").TextContent, Is.EqualTo(SampleEval.Beta));
        Snapshots.Match("Pages/Admin/Evals.live.html", Stable(cut.Markup));
    }

    /// <summary>The runs table in the details names the eval, never the page's own code.</summary>
    [Test]
    public void TheRunsHeading_NamesTheEval()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var served = Serve(ctx);

        var cut = ctx.Render<EvalsPage>();
        cut.WaitForAssertion(() => Assert.That(served.Lists, Is.EqualTo(2)), TimeSpan.FromSeconds(10));

        Assert.That(cut.Find(".eval-pane .runs-list strong").TextContent, Is.EqualTo($"Runs · {SampleEval.Beta}"));
    }

    /// <summary>The eval's manual, opened from its details.</summary>
    [Test]
    public void TheManual_OpensFromTheDetails()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var served = Serve(ctx);
        var cut = ctx.Render<EvalsPage>();
        cut.WaitForAssertion(() => Assert.That(served.Lists, Is.EqualTo(2)), TimeSpan.FromSeconds(10));

        cut.Find(".eval-details .manual-button").Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".manual-dialog"), Has.Count.EqualTo(1)));
        Snapshots.Match("Pages/Admin/Evals.manual.html",
            cut.Find(".manual-dialog").OuterHtml);
    }

    /// <summary>How often the page asked for the list.</summary>
    private sealed class Served
    {
        public int Lists;
    }

    /// <summary>
    /// The services the page injects, over an API that serves the list, the live run, its store and a manual.
    /// </summary>
    private static Served Serve(BunitContext ctx)
    {
        var served = new Served();
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.AbsolutePath;
            if (url.EndsWith("/v1/admin/evals"))
            {
                Interlocked.Increment(ref served.Lists);
                return Json(List);
            }

            if (url.EndsWith("/runs/current")) return Json(Current);
            if (url.EndsWith("/runs/live1")) return Json(Stored);
            if (url.EndsWith("/runs/live1/events")) return Text(Events);
            if (url.EndsWith($"/{SampleEval.Beta}/manual")) return Json(Manual);
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
        // The open turn's view speaks as the app does.
        ctx.Services.AddSingleton(new ChatText());
        return served;
    }

    /// <summary>The sample names filled into a test's JSON.</summary>
    private static string Fill(string text) =>
        SampleIdentity.Fill(SampleEval.Fill(SampleMode.Fill(SampleRole.Fill(text))));

    /// <summary>A 200 with this JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>A 200 with this stream of server-sent events.</summary>
    private static HttpResponseMessage Text(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body.ReplaceLineEndings("\n"), Encoding.UTF8, "text/event-stream")
        };

    /// <summary>
    /// The markup made the same on any machine: each chart title's note id (fresh on every render), and the times,
    /// which the page writes in the reader's own time zone (CI runs in UTC).
    /// </summary>
    private static string Stable(string markup) =>
        ClockTime().Replace(
            RowTime().Replace(LocalTime().Replace(TipId().Replace(markup, "tip-ID"), "$1, HH:mm"), "$1 HH:mm"),
            "started HH:mm:ss");

    /// <summary>A day and a time, as the details write them: "24 Oct 2026, 11:00".</summary>
    [GeneratedRegex(@"(\d{1,2} [A-Z][a-z]{2} \d{4}), \d{2}:\d{2}")]
    private static partial Regex LocalTime();

    /// <summary>A date and a time of day, as a run's row writes them: "2026-10-24 11:00".</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}")]
    private static partial Regex RowTime();

    /// <summary>When the live run started, as its panel says it: "started 12:00:00".</summary>
    [GeneratedRegex(@"started \d{2}:\d{2}:\d{2}")]
    private static partial Regex ClockTime();

    /// <summary>A note's id: "tip-" and eight hex digits.</summary>
    [GeneratedRegex("tip-[0-9a-f]{8}")]
    private static partial Regex TipId();
}
