using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Api;

/// <summary>
/// The API over HTTP, as <c>benchmarks/puls_check/client.py</c> calls it (F18 step 5.1). It owns the <see cref="HttpClient"/> it is
/// given (<see cref="CreateHttp"/> builds the runner's); the stream is read with the session-stream reader (ADR 0030) as the headers
/// arrive, so nothing waits for a whole turn. A status that is not a success throws <see cref="EvalApiException"/>.
/// </summary>
public sealed class HttpSessionClient(HttpClient http, TimeSpan? streamIdle = null) : ISessionClient
{
    private const string DefaultApi = "http://127.0.0.1:5070/";

    /// <summary>How long a turn's stream may stay silent: the Python's 600 seconds, per read. <c>HttpClient.Timeout</c> stops counting once the headers arrive.</summary>
    public static readonly TimeSpan DefaultStreamIdle = TimeSpan.FromMinutes(10);

    private readonly TimeSpan _streamIdle = streamIdle ?? DefaultStreamIdle;

    /// <summary>
    /// The runner's one client: <paramref name="api"/> (PULS_API; http://127.0.0.1:5070/ by default), ten minutes for a turn (a cook turn takes
    /// minutes), and <c>x-api-key</c> when <paramref name="key"/> (AGENT_API_KEY) is set.
    /// </summary>
    public static HttpClient CreateHttp(string? api, string? key, HttpMessageHandler? handler = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler);
        client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(api) ? DefaultApi : api.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromMinutes(10);
        if (!string.IsNullOrEmpty(key)) client.DefaultRequestHeaders.Add("x-api-key", key);
        return client;
    }

    public async Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("v1/toolsets", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        // The API names the list "toolsets"; each entry says whether this instance has its settings.
        var reply = await response.Content.ReadFromJsonAsync<SessionWire.ToolsetsReply>(EvalJson.SnakeCase, cancellationToken);
        return reply!.Toolsets.Where(t => t.Available).Select(t => t.Name).ToList();
    }

    public async Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("v1/sessions", Body(request), EvalJson.SnakeCaseNoNulls, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var opened = await response.Content.ReadFromJsonAsync<SessionWire.Opened>(EvalJson.SnakeCase, cancellationToken);
        return opened!.SessionId;
    }

    public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var send = new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{Uri.EscapeDataString(session)}/messages")
        {
            Content = JsonContent.Create(new SessionWire.MessageBody(message), options: EvalJson.SnakeCaseNoNulls),
        };
        using var response = await http.SendAsync(send, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        // An idle limit on the stream's silence: a stream that stalls after its headers ends the turn as a timeout (TurnFailures maps the
        // cancellation to Timeout, as the run's own token is not the one that fired) instead of hanging the run. The clock is paused while the
        // caller holds an event, and restarts when it asks for the next one, so the caller's own work is not counted.
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

    public async Task CloseAsync(string session, CancellationToken cancellationToken)
    {
        using var response = await http.DeleteAsync($"v1/sessions/{Uri.EscapeDataString(session)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static SessionWire.OpenBody Body(SessionRequest request) =>
        new(request.Mode.Toolsets, request.Mode.Role, request.TimeZone, request.Task, request.RunTag, request.Place, request.Mode.Llm,
            new SessionWire.ReplayBody(request.Replay.Web, request.Replay.Of, request.Replay.Model));

    /// <summary>The API's own message (<c>{"error":{"message"}}</c>) when the body has one, else the body's text, else the reason phrase.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        string reason = ApiMessage(text) ?? (text.Trim().Length > 0 ? text.Trim() : response.ReasonPhrase ?? "");
        throw new EvalApiException((int)response.StatusCode, reason);
    }

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
