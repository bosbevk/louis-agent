using System.Net;
using System.Text;
using System.Text.Json;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Sequences;

/// <summary>
/// This instance and a partner's server in one pretend handler, for a sequence's tests: each session gets its own
/// number (sess_1, sess_2, …) and answers "Answer N", unless the test says that session fails; the partner lists a
/// shop and a bakery. What each session was opened as and sent is kept, so a test reads which step got what.
/// </summary>
internal sealed class SequencePeer
{
    /// <summary>This instance, named "self", and the partner's server.</summary>
    public static readonly Dictionary<string, Uri> Servers = new()
    {
        ["self"] = new Uri("http://self.test/"),
        ["partner"] = new Uri("http://partner.test/")
    };

    private int _created;

    /// <summary>The numbers of the sessions whose answer fails, as a model that refused would.</summary>
    public HashSet<int> Failing { get; } = [];

    /// <summary>The handler that plays both servers.</summary>
    public StubHandler Handler { get; }

    /// <summary>Starts the pretend servers.</summary>
    public SequencePeer() => Handler = new StubHandler(Respond);

    /// <summary>Each session opened, as its server and the agent it opened as: "self testprod".</summary>
    public IReadOnlyList<string> Opened => Handler.Requests
        .Where(request => request.Method == HttpMethod.Post && new Uri(request.Url).AbsolutePath == "/v1/sessions")
        .Select(request => $"{new Uri(request.Url).Host.Split('.')[0]} {AgentOf(request.Body!)}")
        .ToList();

    /// <summary>Each message sent to a session, in order.</summary>
    public IReadOnlyList<string> Sent => Handler.Requests
        .Where(request => request.Method == HttpMethod.Post && request.Url.EndsWith("/messages"))
        .Select(request => JsonDocument.Parse(request.Body!).RootElement.GetProperty("message").GetString()!)
        .ToList();

    /// <summary>
    /// How the servers answer: the partner's identities, a new numbered session, its answer or its failure, and the
    /// delete.
    /// </summary>
    private HttpResponseMessage Respond(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (path == "/v1/identities")
            return Json(HttpStatusCode.OK, """{"identities":[{"name":"shop"},{"name":"bakery"}]}""");
        if (request.Method == HttpMethod.Delete) return Json(HttpStatusCode.OK, """{"deleted":true}""");
        if (path == "/v1/sessions")
            return Json(HttpStatusCode.Created, $$"""{"session_id":"sess_{{++_created}}"}""");

        int session = int.Parse(path.Split('/')[3]["sess_".Length..]);
        string stream = Failing.Contains(session) ? FailedStream : Said($"Answer {session}");
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
        };
    }

    /// <summary>The agent a session was opened as: its identity, and its role when it has one.</summary>
    private static string AgentOf(string body)
    {
        var opening = JsonDocument.Parse(body).RootElement;
        string identity = opening.GetProperty("identity").GetString()!;
        return opening.TryGetProperty("role", out var role) && role.ValueKind == JsonValueKind.String &&
               role.GetString()!.Contains('.')
            ? role.GetString()!
            : identity;
    }

    /// <summary>A child's whole answer with the text given, and what it used.</summary>
    private static string Said(string text) => $$$"""
                                                event: content_block_delta
                                                data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"{{{text}}}"}}

                                                event: message_delta
                                                data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":10,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":2,"requests":1,"cost":0.001,"currency":"USD"}}

                                                event: message_stop
                                                data: {"type":"message_stop"}

                                                """;
}
