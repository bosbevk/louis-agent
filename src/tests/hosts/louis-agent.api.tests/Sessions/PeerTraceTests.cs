using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Telemetry;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// The peer's half of a sub-session's trace (F52 step 4): a message that arrives with a W3C <c>traceparent</c> runs
/// its turn in that trace, so a specialist's <c>invoke_agent</c> sits under the parent's tool call that sent it.
/// ASP.NET Core reads the header on its own and makes its request span a child of it; this proves the turn's span
/// is a child of that request.
/// </summary>
public class PeerTraceTests
{
    /// <summary>The trace the parent's request was in, as its header gives it.</summary>
    private const string TraceId = "0af7651916cd43dd8448eb211c80319c";

    /// <summary>The span of the parent's request, as its header gives it.</summary>
    private const string ParentSpanId = "b7ad6b7169203331";

    /// <summary>
    /// A message sent with a parent's <c>traceparent</c>: the request span is the parent's child, and the turn's span
    /// the request's.
    /// </summary>
    [Test]
    public async Task AMessageWithATraceParent_RunsItsTurnInThatTrace()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                lock (spans) spans.Add(a);
            },
        };
        ActivitySource.AddActivityListener(listener);

        // The API on a model that says hello, and a session on it.
        await using var app = ApiTestHost.Build(
            model: new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hallo"))));
        await app.StartAsync();
        var client = app.GetTestClient();
        string id = await OpenSessionAsync(client);

        // The message as the parent's HttpClient sends it: the test client writes no traceparent, so the test does.
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/v1/sessions/{id}/messages")
        {
            Content = JsonContent.Create(new { message = "Hallo?" })
        };
        message.Headers.Add("traceparent", $"00-{TraceId}-{ParentSpanId}-01");
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
        await response.Content.ReadAsStringAsync();

        var (request, turn) = await RequestAndTurnAsync(spans);
        Assert.That(request.OperationName, Is.EqualTo("Microsoft.AspNetCore.Hosting.HttpRequestIn"));
        Assert.That(request.ParentSpanId.ToHexString(), Is.EqualTo(ParentSpanId));
        Assert.That(turn.ParentSpanId, Is.EqualTo(request.SpanId));
    }

    /// <summary>Opens a session with the file tools and hands back its id.</summary>
    private static async Task<string> OpenSessionAsync(HttpClient client)
    {
        using var opened =
            await client.PostAsJsonAsync("/v1/sessions", new { toolsets = new[] { TestToolset.Files.Wire() } });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>
    /// The message's request span and its turn's span in the parent's trace, waiting up to two seconds for the
    /// request's to end after its response.
    /// </summary>
    private static async Task<(Activity Request, Activity Turn)> RequestAndTurnAsync(List<Activity> spans)
    {
        for (int i = 0; i < 200; i++)
        {
            List<Activity> inTrace;
            lock (spans) inTrace = [.. spans.Where(a => a.TraceId.ToHexString() == TraceId)];
            var turn = inTrace.FirstOrDefault(a =>
                a.GetTagItem(SpanTag.OperationName.Wire()) as string == "invoke_agent");
            var request = inTrace.FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");
            if (turn is not null && request is not null) return (request, turn);
            await Task.Delay(10);
        }

        Assert.Fail("the request's and the turn's spans never both ended in the parent's trace");
        throw new UnreachableException();
    }
}
