using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Web.Admin.Runs;
using LouisAgent.Web.Components.Admin.Evals.LiveRun;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Admin.Evals.LiveRun;

/// <summary>
/// The live run's panel asks for the costs so far each time a turn ends (FX34), and a question that fails on the way
/// (the API restarting, the network dropping) never stops the next turn's from being asked.
/// </summary>
public class LiveRunPanelTests
{
    /// <summary>When the run's events happened; the panel doesn't read it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The run being followed, still going.</summary>
    private static readonly LiveRunRow Running =
        new("live1", SampleEval.Beta, 1, "running", null, T0, null, 0, null, null, null, null, null);

    /// <summary>
    /// The first question for the costs fails before it reaches the API; the second turn's end asks again, and the
    /// first turn's cost shows.
    /// </summary>
    [Test]
    public void AFailedQuestion_DoesntStopTheNextTurnsCosts()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        int asked = 0;
        Serve(ctx, () => ++asked == 1
            ? throw new HttpRequestException("the network dropped")
            : Json(JsonSerializer.Serialize(new LiveRequestsReply([Request(1, 0.25m)]), AdminJson.SnakeCase)));

        var cut = ctx.Render<LiveRunPanel>(p => p.Add(x => x.Run, Running));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".run-table tbody td:nth-child(7)")[0].TextContent,
            Is.EqualTo(RunText.Money(0.25m, null))), TimeSpan.FromSeconds(5));
        Assert.That(asked, Is.GreaterThanOrEqualTo(2));
    }

    /// <summary>One request of bobotie's turn, at this cost.</summary>
    private static RequestRow Request(int turn, decimal cost) =>
        new(new TurnKeyRow(SampleMode.Alpha, 1, "bobotie", turn), T0, "turn", "s1", null, "claude-haiku-5-5", 1000,
            200, 0, 0, cost, 1500);

    /// <summary>
    /// The services the panel injects, over an API whose run streams two finished turns of bobotie, answers its
    /// requests so far with <paramref name="requests"/>, and has nothing stored yet.
    /// </summary>
    private static void Serve(BunitContext ctx, Func<HttpResponseMessage> requests)
    {
        var handler = new StubHandler(request =>
        {
            string url = request.RequestUri!.AbsolutePath;
            if (url.EndsWith("/runs/live1/events")) return Events();
            if (url.EndsWith("/runs/live1/requests")) return requests();
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                    Encoding.UTF8, "application/json")
            };
        });

        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(
            new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings));
        // The open turn's view speaks as the app does.
        ctx.Services.AddSingleton(new ChatText());
    }

    /// <summary>The run's two turns, each asked and ended, as server-sent events.</summary>
    private static HttpResponseMessage Events()
    {
        EvalEvent[] events =
        [
            new QuestionEvent(T0, SampleMode.Alpha, 1, "bobotie", 1, "s1", "Maak bobotie"),
            new TurnEvent(T0, SampleMode.Alpha, 1, "bobotie", 1, "s1", "Maak bobotie", "Hier", null, null, 1, 0, [],
                2.0, null, null),
            new QuestionEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "s1", "En vir ses?"),
            new TurnEvent(T0, SampleMode.Alpha, 1, "bobotie", 2, "s1", "En vir ses?", "Verdubbel", null, null, 1, 0,
                [], 2.0, null, null),
        ];
        string body = string.Join("\n\n", events.Select(e => "data: " + EvalProtocolJson.Line(e))) + "\n\n";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
        };
    }

    /// <summary>A 200 with this JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
