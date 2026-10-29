using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Api;

/// <summary>
/// Talks to the real API over HTTP, the way the old Python check did: opens a session, sends each question and reads
/// the answer's stream as it arrives, so nothing waits for a whole turn. When the API says no, you get an
/// <see cref="EvalApiException"/> with the API's own reason.
/// </summary>
/// <remarks>
/// It owns the <see cref="HttpClient"/> it is handed; <see cref="CreateHttp"/> builds the one the runner uses. The
/// stream is read with the shared session-stream reader rather than our own parser.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0030-build-or-adopt.md">ADR 0030: build or adopt</seealso>
public sealed class HttpSessionClient(HttpClient http, TimeSpan? streamIdle = null) : ISessionClient
{
    /// <summary>Where the API runs when no address is set: Puls on this machine.</summary>
    private const string DefaultApi = "http://127.0.0.1:5070/";

    /// <summary>
    /// How long a turn's stream may stay silent before we give up: ten minutes per read, as the Python had it.
    /// <c>HttpClient.Timeout</c> stops counting once the headers arrive, so this is what guards the stream.
    /// </summary>
    public static readonly TimeSpan DefaultStreamIdle = TimeSpan.FromMinutes(10);

    private readonly TimeSpan _streamIdle = streamIdle ?? DefaultStreamIdle;

    /// <summary>
    /// Where the agent API is: <c>AGENT_API</c>, else <c>PULS_API</c>, its old name, read until a TODO line removes
    /// it. Nothing when neither is set, and the client takes its default.
    /// </summary>
    public static string? ApiFrom(Func<string, string?> getVariable) =>
        new[] { getVariable("AGENT_API"), getVariable("PULS_API") }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>
    /// Builds the runner's client: the API at <paramref name="api"/> (Puls on this machine by default), ten minutes
    /// for a turn since a cook turn takes minutes, and the <c>x-api-key</c> header when there is a key.
    /// </summary>
    public static HttpClient CreateHttp(string? api, string? key, HttpMessageHandler? handler = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler);
        client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(api) ? DefaultApi : api.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromMinutes(10);
        if (!string.IsNullOrEmpty(key)) client.DefaultRequestHeaders.Add("x-api-key", key);
        return client;
    }

    /// <summary>The toolsets this instance can actually offer, because it has their settings.</summary>
    public async Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("v1/toolsets", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        // The API names the list "toolsets"; each entry says whether this instance has its settings.
        var reply = await response.Content.ReadFromJsonAsync<SessionWire.ToolsetsReply>(EvalJson.SnakeCase,
            cancellationToken);
        return reply!.Toolsets.Where(t => t.Available).Select(t => t.Name).ToList();
    }

    /// <summary>Opens a session for one conversation and hands back its id.</summary>
    public async Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
    {
        using var response =
            await http.PostAsJsonAsync("v1/sessions", Body(request), EvalJson.SnakeCaseNoNulls, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var opened =
            await response.Content.ReadFromJsonAsync<SessionWire.Opened>(EvalJson.SnakeCase, cancellationToken);
        return opened!.SessionId;
    }

    /// <summary>
    /// Sends one question and hands you the answer's events as they come. A stream that goes quiet for too long ends
    /// the turn instead of hanging the run.
    /// </summary>
    public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Ask for the headers only, so the stream can be read while the answer is still being written.
        using var send =
            new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{Uri.EscapeDataString(session)}/messages")
            {
                Content = JsonContent.Create(new SessionWire.MessageBody(message), options: EvalJson.SnakeCaseNoNulls),
            };
        using var response = await http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        // An idle limit on the stream's silence: a stream that stalls after its headers ends the turn as a timeout
        // (TurnFailures maps the cancellation to Timeout, as the run's own token is not the one that fired) instead of
        // hanging the run. The clock is paused while the caller holds an event, and restarts when it asks for the
        // next one, so the caller's own work is not counted.
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idle.CancelAfter(_streamIdle);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, idle.Token))
        {
            idle.CancelAfter(Timeout.InfiniteTimeSpan);
            yield return streamEvent;
            idle.CancelAfter(_streamIdle);
        }
    }

    /// <summary>Closes the session once its conversation is over.</summary>
    public async Task CloseAsync(string session, CancellationToken cancellationToken)
    {
        using var response = await http.DeleteAsync($"v1/sessions/{Uri.EscapeDataString(session)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Turns the session we want into the body the API expects when a session opens.</summary>
    private static SessionWire.OpenBody Body(SessionRequest request) =>
        new(request.Mode.Toolsets, request.Mode.Role, request.TimeZone, request.Task, request.RunTag, request.Place,
            request.Mode.Llm,
            new SessionWire.ReplayBody(request.Replay.Web, request.Replay.Of, request.Replay.Model));

    /// <summary>
    /// Lets a good answer through, and turns any other into an exception that says why the API said no.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new EvalApiException((int)response.StatusCode, Reason(text, response.ReasonPhrase));
    }

    /// <summary>
    /// The best reason we can give: the API's own message if the body has one, else the body's text, else the
    /// status's reason phrase.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): one source of the reason per line, the best first.</remarks>
    private static string Reason(string text, string? reasonPhrase)
    {
        if (ApiMessage(text) is { } message) return message;
        if (text.Trim().Length > 0) return text.Trim();
        return reasonPhrase ?? "";
    }

    /// <summary>The message in the API's error body (<c>{"error":{"message"}}</c>), if the body is one.</summary>
    private static string? ApiMessage(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<SessionWire.ErrorReply>(text, EvalJson.SnakeCase)?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
