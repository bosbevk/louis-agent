using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Puls.Text;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin;

/// <summary>
/// Pins what the run explorer shows at every depth over a rich stored run: the overview's tiles, bars and rings, the
/// conversations (all, and only those to review), one conversation, one turn, one request, a turn replayed as typed,
/// the chats side by side, and the second repeat. The explorer's rebuild into components keeps it (goldens:
/// <c>RunExplorer.*.html</c>).
/// </summary>
public partial class RunExplorerSnapshotTests
{
    /// <summary>
    /// Two modes over two repeats: graded conversations (one with a review waiting, one a cook without a list),
    /// turns with roles, tools and the sites they read, priced requests (one a specialist's, one with no price, one
    /// with no turn), and notes on the run and on a conversation.
    /// </summary>
    private static readonly string Run = SampleMode.Fill(SampleRole.Fill(SampleToolset.Fill("""
        {"run":{"id":"run7","check":"puls-cook","repeat":2,"status":"done","started_at":"2026-10-27T10:00:00+00:00",
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule","question":null},{"name":"meal-for-people","version":1,"kind":"Model","question":null},
                           {"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}]},
         "modes":[{"name":"<ModeAlpha>","repeat":1,"toolsets":["<Alpha>"]},{"name":"<ModeBeta>","repeat":1,"toolsets":["<Alpha>","<Beta>"]},
                  {"name":"<ModeAlpha>","repeat":2,"toolsets":["<Alpha>"]},{"name":"<ModeBeta>","repeat":2,"toolsets":["<Alpha>","<Beta>"]}],
         "notes":[{"text":"The shops were slow today.","at":"2026-10-27T11:00:00+00:00","conversation":null},
                  {"text":"Priced the rice per kilo.","at":"2026-10-27T11:05:00+00:00","conversation":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie"}}],
         "agreement":[],
         "conversations":[
           {"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","order":0,"role":"<RoleAlpha>","errors":1,"seconds":7.5,"outcome":"Pass",
            "scores":{"routed_right":true,"follow_up":"continued","ingredients":5,"priced":3,"estimated":1,"priced_ratio":0.8,"images":1,"unsourced":1,"per_kg":1,
                      "deals_named":2,"untooled_shops":1,"no_shop_rows":1,"wrong_store":1,"no_catalogue_line":true,"store_for_area":2,"store_default":1},
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds four"},
                      {"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}],
            "human_grades":[]},
           {"mode":"<ModeAlpha>","repeat":1,"task":"cook-curry","order":1,"role":null,"errors":0,"seconds":2,"outcome":"Fail",
            "scores":{"routed_right":false,"follow_up":"new","ingredients":null},
            "grades":[{"grader":"no-error","version":1,"score":0,"outcome":"Fail","reason":"an error"}],"human_grades":[]},
           {"mode":"<ModeBeta>","repeat":1,"task":"bobotie","order":0,"role":"<RoleAlpha>","errors":0,"seconds":5,"outcome":"Pass",
            "scores":{"routed_right":true,"ingredients":4,"priced":4,"priced_ratio":1.0,"images":0},
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds four"},
                      {"grader":"meal-for-people.human","version":1,"score":1,"outcome":"Pending","reason":"waits for a person"}],
            "human_grades":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","grader":"meal-for-people.human","model_grader":"meal-for-people","verdict":"pass","note":"good","reviewer":"ann","at":"2026-10-27T12:00:00+00:00","latest":true}]},
           {"mode":"<ModeBeta>","repeat":1,"task":"chat","order":1,"role":null,"errors":0,"seconds":1,"outcome":null,"scores":{"routed_right":true},"grades":null,"human_grades":[]},
           {"mode":"<ModeAlpha>","repeat":2,"task":"bobotie","order":0,"role":null,"errors":0,"seconds":3,"outcome":null,"scores":{"routed_right":false},"grades":null,"human_grades":[]},
           {"mode":"<ModeBeta>","repeat":2,"task":"bobotie","order":0,"role":null,"errors":0,"seconds":3,"outcome":null,"scores":{},"grades":null,"human_grades":[]}],
         "turns":[
           {"tools":{"calls":4,"failed":2,"reasons":[{"tool":"WebSearch","reason":"refused by the site","count":2}]},"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1,"session":"s1",
            "question":"What can I cook tonight for four people with mince, rice and a little curry powder?","answer":"Bobotie for four.","role":"<RoleAlpha>","continued":null,
            "agent_calls":1,"specialist_calls":2,"hosts":["www.checkers.co.za","pnp.co.za","food.co.za","www.openstreetmap.org","example.com"],"seconds":6.5,"error":null},
           {"tools":{"calls":1,"failed":0,"reasons":[]},"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":2,"session":"s1","question":"And for six?","answer":"Double it.",
            "role":null,"continued":"sub-1","agent_calls":1,"specialist_calls":0,"hosts":[],"seconds":1,"error":null},
           {"tools":{"calls":0,"failed":0,"reasons":[]},"mode":"<ModeAlpha>","repeat":1,"task":"cook-curry","turn":1,"session":"s2","question":"Curry?","answer":null,
            "role":null,"continued":null,"agent_calls":1,"specialist_calls":0,"hosts":[],"seconds":2,"error":"the model stopped"},
           {"tools":{"calls":2,"failed":1,"reasons":[{"tool":"Fetch","reason":"timed out","count":1}]},"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"s3",
            "question":"What can I cook tonight for four people with mince, rice and a little curry powder?","answer":"Bobotie, priced.","role":"<RoleAlpha>","continued":null,
            "agent_calls":1,"specialist_calls":1,"hosts":["woolworths.co.za"],"seconds":5,"error":null},
           {"tools":{"calls":0,"failed":0,"reasons":[]},"mode":"<ModeBeta>","repeat":1,"task":"chat","turn":1,"session":"s4","question":"Hello","answer":"Hi.",
            "role":null,"continued":null,"agent_calls":1,"specialist_calls":0,"hosts":[],"seconds":1,"error":null},
           {"tools":{"calls":0,"failed":0,"reasons":[]},"mode":"<ModeAlpha>","repeat":2,"task":"bobotie","turn":1,"session":"s5","question":"Bobotie again?","answer":"Yes.",
            "role":null,"continued":null,"agent_calls":1,"specialist_calls":0,"hosts":[],"seconds":3,"error":null},
           {"tools":{"calls":0,"failed":0,"reasons":[]},"mode":"<ModeBeta>","repeat":2,"task":"bobotie","turn":1,"session":"s6","question":"Bobotie again?","answer":"Sure.",
            "role":null,"continued":null,"agent_calls":1,"specialist_calls":0,"hosts":[],"seconds":3,"error":null}],
         "requests":[
           {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:00:02.500+00:00","purpose":"turn","session":"s1","parent":null,"model":"claude-haiku",
            "input_tokens":100,"output_tokens":50,"cache_read_tokens":400,"cache_write_tokens":20,"cost":0.012,"duration_ms":1500},
           {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:00:05+00:00","purpose":"turn","session":"sub-1","parent":"s1","model":"claude-sonnet",
            "input_tokens":200,"output_tokens":80,"cache_read_tokens":0,"cache_write_tokens":300,"cost":0.034,"duration_ms":2000},
           {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":2},"at":"2026-10-27T10:01:00+00:00","purpose":"turn","session":"s1","parent":null,"model":null,
            "input_tokens":null,"output_tokens":10,"cache_read_tokens":null,"cache_write_tokens":null,"cost":null,"duration_ms":400},
           {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"cook-curry","turn":1},"at":"2026-10-27T10:02:00+00:00","purpose":"turn","session":"s2","parent":null,"model":"claude-haiku",
            "input_tokens":50,"output_tokens":5,"cache_read_tokens":0,"cache_write_tokens":0,"cost":0.001,"duration_ms":300},
           {"turn":{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:03:00+00:00","purpose":"turn","session":"s3","parent":null,"model":"claude-haiku",
            "input_tokens":120,"output_tokens":60,"cache_read_tokens":500,"cache_write_tokens":0,"cost":0.008,"duration_ms":1200},
           {"turn":{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1},"at":"2026-10-27T10:03:04+00:00","purpose":"turn","session":"sub-3","parent":"s3","model":"claude-sonnet",
            "input_tokens":90,"output_tokens":40,"cache_read_tokens":100,"cache_write_tokens":50,"cost":0.02,"duration_ms":2500},
           {"turn":{"mode":"<ModeBeta>","repeat":1,"task":"chat","turn":1},"at":"2026-10-27T10:04:00+00:00","purpose":"turn","session":"s4","parent":null,"model":"claude-haiku",
            "input_tokens":10,"output_tokens":3,"cache_read_tokens":0,"cache_write_tokens":0,"cost":0.0005,"duration_ms":200},
           {"turn":{"mode":"<ModeAlpha>","repeat":2,"task":"bobotie","turn":1},"at":"2026-10-27T10:05:00+00:00","purpose":"turn","session":"s5","parent":null,"model":"claude-haiku",
            "input_tokens":30,"output_tokens":7,"cache_read_tokens":0,"cache_write_tokens":0,"cost":0.002,"duration_ms":500},
           {"turn":null,"at":"2026-10-27T10:06:00+00:00","purpose":"judge","session":null,"parent":null,"model":"claude-haiku",
            "input_tokens":10,"output_tokens":1,"cache_read_tokens":0,"cache_write_tokens":0,"cost":0.0001,"duration_ms":100}]}
        """)));

    /// <summary>The run as it opens: the overview.</summary>
    [Test]
    public void TheOverview_IsShownAsBefore() => Match(Render(null), "overview");

    /// <summary>The second repeat's overview.</summary>
    [Test]
    public void TheSecondRepeat_IsShownAsBefore()
    {
        var cut = Render(null);

        cut.FindAll(".run-controls .segmented button").First(b => b.TextContent == "2").Click();

        Match(cut, "repeat2");
    }

    /// <summary>The conversations, all of them.</summary>
    [Test]
    public void TheConversations_AreShownAsBefore()
    {
        var cut = Render(null);

        cut.Find(".tile").Click();

        Match(cut, "conversations");
    }

    /// <summary>The conversations, only those still to review.</summary>
    [Test]
    public void TheConversationsToReview_AreShownAsBefore()
    {
        var cut = Render(null);
        cut.Find(".tile").Click();

        cut.Find("[data-filter=to-review]").Click();

        Match(cut, "conversations.to-review");
    }

    /// <summary>A task opened from a category's line: its turns, the review and the notes.</summary>
    [Test]
    public void AnOpenConversation_IsShownAsBefore() => Match(Render("bobotie"), "conversation");

    /// <summary>One turn's requests and transcript.</summary>
    [Test]
    public void ATurn_IsShownAsBefore()
    {
        var cut = Render("bobotie");

        cut.FindAll(".turn-open.quiet").First().Click();

        Match(cut, "turn");
    }

    /// <summary>One request of a turn: a specialist's, so its lane is named for the role.</summary>
    [Test]
    public void ARequest_IsShownAsBefore()
    {
        var cut = Render("bobotie");
        cut.FindAll(".turn-open.quiet").First().Click();

        cut.FindAll(".tl-bar").Last(b => !b.HasAttribute("disabled")).Click();

        Match(cut, "request");
    }

    /// <summary>A turn replayed as typed, once the replay has played to its end.</summary>
    [Test]
    public void AReplayedTurn_IsShownAsBefore()
    {
        var cut = Render("bobotie");
        cut.FindAll(".turn-open.quiet").First().Click();

        cut.Find(".replay-bar button").Click();

        // A state, not an assertion: NUnit keeps every failed assertion it saw, even one a later render passes.
        cut.WaitForState(() => Played(cut) && cut.FindAll(".replay-bar .working").Count == 0,
            TimeSpan.FromSeconds(10));
        Match(cut, "replay");
    }

    /// <summary>The chats side by side, once they have played to their end.</summary>
    [Test]
    public void TheChats_AreShownAsBefore()
    {
        var cut = Render(null);

        cut.Find(".chat-open").Click();

        cut.WaitForState(() => Played(cut) && cut.FindAll(".chats-dialog .working").Count == 0,
            TimeSpan.FromSeconds(10));
        Match(cut, "chats");
    }

    /// <summary>Both modes' replays have played their last words.</summary>
    private static bool Played(IRenderedComponent<RunExplorer> cut) =>
        cut.Markup.Contains("Alpha says bobotie.") && cut.Markup.Contains("Beta says bobotie.");

    /// <summary>Renders the explorer on the run, in US dollars, opened at a task if one is given.</summary>
    private static IRenderedComponent<RunExplorer> Render(string? openTask)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // What the explorer and its panels need: an API that serves a turn's events, a reviewer, the notes state and
        // the app's words for the turn's view.
        var api = new AgentApi(new HttpClient(new StubHandler(Serve)) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings());
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());
        ctx.Services.AddSingleton(PulsText.Afrikaans);

        return ctx.Render<RunExplorer>(p =>
            p.Add(x => x.Run, JsonDocument.Parse(Run).RootElement.Clone()).Add(x => x.OpenTask, openTask));
    }

    /// <summary>
    /// A turn's stored events: a text block that streams in two pieces, said by the mode that played it; anything
    /// else is not found.
    /// </summary>
    private static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        string url = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
        if (!url.EndsWith("/events"))
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                    Encoding.UTF8, "application/json")
            };

        string who = url.Contains(SampleMode.Alpha) ? "Alpha" : "Beta";
        string body = """
                      {"events":[
                       {"type":"content_block_start","at":"2026-10-27T10:00:00.000+00:00","data":{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}},
                       {"type":"content_block_delta","at":"2026-10-27T10:00:00.030+00:00","data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"<Who> says "}}},
                       {"type":"content_block_delta","at":null,"data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"bobotie."}}},
                       {"type":"content_block_stop","at":"2026-10-27T10:00:00.060+00:00","data":{"type":"content_block_stop","index":0}}]}
                      """;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body.Replace("<Who>", who), Encoding.UTF8, "application/json")
        };
    }

    /// <summary>The explorer's markup, made the same on any machine, against its golden.</summary>
    private static void Match(IRenderedComponent<RunExplorer> cut, string state) =>
        Snapshots.Match("louis-agent.puls.tests", $"Components/Admin/RunExplorer.{state}.html", Stable(cut.Markup));

    /// <summary>
    /// The markup without what changes between renders and machines: each chart title's note id (fresh on every
    /// render), and the request and note times, which the explorer writes in the reader's own time zone (CI runs in
    /// UTC).
    /// </summary>
    private static string Stable(string markup) =>
        NoteTime().Replace(ClockTime().Replace(TipId().Replace(markup, "tip-ID"), "HH:mm:ss"), "$1 HH:mm");

    /// <summary>A chart title's note id.</summary>
    [GeneratedRegex("tip-[0-9a-f]{8}")]
    private static partial Regex TipId();

    /// <summary>A time of day, with or without its milliseconds.</summary>
    [GeneratedRegex(@"\b\d{2}:\d{2}:\d{2}(\.\d{3})?\b")]
    private static partial Regex ClockTime();

    /// <summary>A date and a time of day with no seconds, as a note's time is written ("2026-10-27 13:05").</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}(?!:)")]
    private static partial Regex NoteTime();
}
