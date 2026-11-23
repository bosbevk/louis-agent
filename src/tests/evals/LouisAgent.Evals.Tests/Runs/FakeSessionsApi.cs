using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Sessions;
using LouisAgent.SessionStream;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// The sessions API, played by a handler: each session it opens gets the next id, each message is answered with the
/// next turn's stream, and every request is written down. A turn given as <see cref="Refused"/> answers 500, as an
/// API that fails mid-conversation does. The site's page route answers every address with a page, but those named
/// missing (F126.3.3).
/// </summary>
internal sealed class FakeSessionsApi
{
    private const string Api = "http://api.test/";
    private const string Sessions = "/v1/sessions";
    private const string PageRoute = "/v1/site/page";

    /// <summary>A turn the API refuses instead of answering.</summary>
    public const string? Refused = null;

    private readonly Queue<string?> _turns;
    private int _opened;

    /// <summary>The API, with the streams of the turns it will answer, in order.</summary>
    public FakeSessionsApi(params string?[] turns)
    {
        _turns = new Queue<string?>(turns);
        Handler = new StubHandler(Answer);
    }

    /// <summary>What answers the client, and what it was asked.</summary>
    public StubHandler Handler { get; }

    /// <summary>A client over this API.</summary>
    public SessionClient Client() => new(new HttpClient(Handler) { BaseAddress = new Uri(Api) });

    /// <summary>The addresses the site has no page at, which the page route answers 404.</summary>
    public HashSet<string> MissingPages { get; } = new(StringComparer.Ordinal);

    /// <summary>A page client over this API, keeping its pages in this store, if any.</summary>
    public PageClient Pages(ReplayStore? recordings = null) =>
        new(new HttpClient(Handler) { BaseAddress = new Uri(Api) }, recordings);

    /// <summary>Each address the page route was asked for, in order.</summary>
    public IReadOnlyList<string> PagesAsked() =>
        Handler.Requests.Where(request => new Uri(request.Url).AbsolutePath == PageRoute)
            .Select(request => AddressOf(new Uri(request.Url))).ToList();

    /// <summary>The page each message carried, by its address, or none, in order.</summary>
    public IReadOnlyList<string?> PagesSent() =>
        Handler.Requests.Where(request => request.Url.EndsWith("/messages", StringComparison.Ordinal))
            .Select(request => JsonDocument.Parse(request.Body!).RootElement.TryGetProperty("page", out var page)
                ? page.GetProperty("url").GetString()
                : null)
            .ToList();

    /// <summary>Each request as its method and its path, in the order they came.</summary>
    public IReadOnlyList<string> Calls() =>
        Handler.Requests.Select(request => $"{request.Method} {new Uri(request.Url).AbsolutePath}").ToList();

    /// <summary>The bodies of the requests that opened a session.</summary>
    public IReadOnlyList<string> Opened() =>
        Handler.Requests.Where(request => new Uri(request.Url).AbsolutePath == Sessions)
            .Select(request => request.Body ?? "").ToList();

    /// <summary>The messages sent, in order.</summary>
    public IReadOnlyList<string> Asked() =>
        Handler.Requests.Where(request => request.Url.EndsWith("/messages", StringComparison.Ordinal))
            .Select(request => JsonDocument.Parse(request.Body!).RootElement.GetProperty("message").GetString()!)
            .ToList();

    /// <summary>A turn the agent answers with this text, priced at this cost.</summary>
    public static string Answered(string text, decimal? cost = 0.001m) =>
        Sse("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text"}}""") +
        Sse("content_block_delta", Delta(text)) +
        Sse("message_delta", Usage(cost)) +
        Sse("message_stop", """{"type":"message_stop"}""");

    /// <summary>A turn that ends in the agent's own error, after it began to answer.</summary>
    public static string Failed(string message) =>
        Sse("content_block_delta", Delta("Let me ")) +
        Sse("error",
            JsonSerializer.Serialize(new ErrorEvent(new ErrorDetail(StreamErrorKind.Api.Wire(), message)), SnakeCase));

    /// <summary>The answer to one request, by its route.</summary>
    private HttpResponseMessage Answer(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Delete) return Json(HttpStatusCode.OK, """{"deleted":true}""");
        if (path == Sessions) return Opens();
        if (path == PageRoute) return PageAt(AddressOf(request.RequestUri));
        return Streams(_turns.Dequeue());
    }

    /// <summary>The page at an address, or 404 when it is named missing.</summary>
    private HttpResponseMessage PageAt(string address)
    {
        if (MissingPages.Contains(address)) return new HttpResponseMessage(HttpStatusCode.NotFound);
        var page = new PageRequest(address, "A page", "Its words.");
        return Json(HttpStatusCode.OK, JsonSerializer.Serialize(page, JsonSerializerOptions.Web));
    }

    /// <summary>The address a call to the page route asks for.</summary>
    private static string AddressOf(Uri route) => Uri.UnescapeDataString(route.Query["?address=".Length..]);

    /// <summary>A new session, with the next id.</summary>
    private HttpResponseMessage Opens()
    {
        _opened++;
        return Json(HttpStatusCode.Created,
            $$"""{"session_id":"s-{{_opened}}","created_at":"2026-11-23T04:30:12Z","toolsets":[],"depth":0}""");
    }

    /// <summary>A turn's stream, or the API's refusal.</summary>
    private static HttpResponseMessage Streams(string? turn) => turn is null
        ? Json(HttpStatusCode.InternalServerError, JsonSerializer.Serialize(GoneError, SnakeCase))
        : new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(turn, Encoding.UTF8, "text/event-stream")
        };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>One event of the stream, as the API writes it.</summary>
    private static string Sse(string type, string data) => $"event: {type}\ndata: {data}\n\n";

    private static string Delta(string text) =>
        JsonSerializer.Serialize(new DeltaEvent(0, new TextDelta(text)), SnakeCase);

    private static string Usage(decimal? cost) =>
        JsonSerializer.Serialize(new UsageEvent(new StopDelta("end_turn"), new UsageNumbers(cost, "USD")), SnakeCase);

    /// <summary>The API's refusal when a session is gone.</summary>
    private static readonly ErrorEvent GoneError =
        new(new ErrorDetail(StreamErrorKind.Api.Wire(), "The session is gone."));

    private static readonly JsonSerializerOptions SnakeCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>A delta event, carrying a piece of text.</summary>
    private sealed record DeltaEvent(int Index, TextDelta Delta);

    /// <summary>A text delta's body.</summary>
    private sealed record TextDelta(string Text)
    {
        public string Type => "text_delta";
    }

    /// <summary>The turn's end, with its usage.</summary>
    private sealed record UsageEvent(StopDelta Delta, UsageNumbers Usage);

    /// <summary>Why the turn stopped.</summary>
    private sealed record StopDelta(string StopReason);

    /// <summary>What the turn cost.</summary>
    private sealed record UsageNumbers(decimal? Cost, string Currency);

    /// <summary>A failed turn's event.</summary>
    private sealed record ErrorEvent(ErrorDetail Error)
    {
        public string Type => "error";
    }

    /// <summary>A failure's kind and message.</summary>
    private sealed record ErrorDetail(string Type, string Message);
}
