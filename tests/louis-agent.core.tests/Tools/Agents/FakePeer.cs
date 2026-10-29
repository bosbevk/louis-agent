using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Tools.Agents;

/// <summary>
/// A pretend peer for the specialist tests, so nothing goes over the network: the streams it answers with, the
/// handlers that play it, and the tools pointed at it.
/// </summary>
internal static class FakePeer
{
    /// <summary>The one peer the tests know: "coder".</summary>
    public static readonly Dictionary<string, Uri> Peers = new() { ["coder"] = new Uri("http://coder.test/") };

    /// <summary>A child's whole answer, in two pieces, with what it used: three requests and a cent.</summary>
    public const string AnswerStream = """
                                       event: message_start
                                       data: {"type":"message_start","message":{"session_id":"sess_child"}}

                                       event: content_block_delta
                                       data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Fixed the null check; "}}

                                       event: content_block_delta
                                       data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"14/14 tests pass."}}

                                       event: message_delta
                                       data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1000,"cache_creation_input_tokens":0,"cache_read_input_tokens":9000,"output_tokens":200,"requests":3,"cost":0.0123,"currency":"USD"}}

                                       event: message_stop
                                       data: {"type":"message_stop"}

                                       """;

    /// <summary>
    /// The same answer as a replay reports it: the same requests, but every one answered from the recording, so no
    /// tokens and no cost (FX27).
    /// </summary>
    public const string ReplayedAnswerStream = """
                                               event: message_start
                                               data: {"type":"message_start","message":{"session_id":"sess_child"}}

                                               event: content_block_delta
                                               data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Fixed the null check; "}}

                                               event: content_block_delta
                                               data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"14/14 tests pass."}}

                                               event: message_delta
                                               data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":0,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":0,"requests":3,"cost":0,"currency":"USD"}}

                                               event: message_stop
                                               data: {"type":"message_stop"}

                                               """;

    /// <summary>A reply with a JSON body and the status given.</summary>
    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>
    /// A peer that creates sess_child, answers with <paramref name="answer"/> and accepts the delete.
    /// </summary>
    public static StubHandler Peer(Func<HttpResponseMessage>? answer = null) => new(request =>
        request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/v1/sessions" =>
                Json(HttpStatusCode.Created, """{"session_id":"sess_child"}"""),
            "POST" => answer?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream")
            },
            "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });

    /// <summary>The specialist tools pointed at the stub peer, with the test's own timeout, clock and ledger.</summary>
    public static SpecialistTools ToolsOn(StubHandler handler, TimeSpan? timeout = null, TimeProvider? time = null,
        IUsageSink? sink = null) =>
        new(Peers, "the-key", new HttpClient(handler), timeout, time, sink);

    /// <summary>A reply that failed after some work: the API's error event carries what was charged (FX6).</summary>
    public const string FailedStream = """
                                       event: error
                                       data: {"type":"error","error":{"type":"api_error","message":"the model refused"},"usage":{"input_tokens":500,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":50,"requests":2,"cost":0.002,"currency":"USD"}}

                                       """;

    /// <summary>
    /// A peer whose sessions get their own ids (sess_1, sess_2, …), so kept-open ones can be told apart.
    /// </summary>
    public static StubHandler PeerWithIds()
    {
        int created = 0;
        return new StubHandler(request => request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/v1/sessions" =>
                Json(HttpStatusCode.Created, $$"""{"session_id":"sess_{{++created}}"}"""),
            "POST" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream")
            },
            "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
    }

    /// <summary>The calls the stub got, as "METHOD /path", in order.</summary>
    public static IEnumerable<string> Calls(StubHandler handler) =>
        handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}");

    /// <summary>
    /// A reply with usage and no text: what the delegation check's specialist sent after its tenth tool round.
    /// </summary>
    public const string EmptyStream = """
                                      event: message_delta
                                      data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":100,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":104,"requests":11,"cost":0.0081,"currency":"USD"}}

                                      event: message_stop
                                      data: {"type":"message_stop"}

                                      """;

    /// <summary>Answers each message with the next of the streams given, in order.</summary>
    public static Func<HttpResponseMessage> Replies(params string[] streams)
    {
        int next = 0;
        return () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streams[next++], Encoding.UTF8, "text/event-stream")
        };
    }

    /// <summary>Creates the child, then never answers the message until cancelled; records the delete.</summary>
    public sealed class SlowPeer : HttpMessageHandler
    {
        /// <summary>Whether the child was deleted.</summary>
        public bool Deleted { get; private set; }

        /// <summary>
        /// Creates the session, notes a delete, and waits on any message until the call is cancelled.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Delete)
            {
                Deleted = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (request.RequestUri!.AbsolutePath == "/v1/sessions")
                return Json(HttpStatusCode.Created, """{"session_id":"sess_child"}""");

            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
