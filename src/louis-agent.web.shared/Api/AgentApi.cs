using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Info;
using LouisAgent.Api.Contracts.Sessions;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LouisAgent.Web.Shared.Api;

/// <summary>
/// How the web apps talk to louis-agent.api, which serves them from the same origin: it opens and ends sessions,
/// streams a turn, and reads and posts the apps' own routes, always with the right key.
/// </summary>
/// <remarks>
/// Gateway (Fowler, PoEAA): every call to the API goes through this one class, so the key, the error shape and the
/// streaming switch are handled in one place. What it sends and reads is a named record: the bodies its own
/// (<see cref="CreateSessionBody"/>, <see cref="MessageBody"/>), the replies the contract's (ADR 0045).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed class AgentApi(HttpClient http, ChatSettings settings)
{
    /// <summary>
    /// Opens a session with the toolsets it keeps (F5): the code-review app asks for every available toolset, Puls
    /// only <c>specialists</c>, since it delegates. The role is its voice (F21; none is the API's default), and the
    /// place is where the user is, only when they shared it (F22).
    /// </summary>
    public async Task<string> CreateSessionAsync(IEnumerable<string> toolsets, string? role = null,
        string? location = null, string? timeZone = null)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/sessions")
        {
            Content = JsonContent.Create(new CreateSessionBody(toolsets, role, location, timeZone)),
        });
        var reply = await response.Content.ReadFromJsonAsync<SessionCreatedReply>(SessionJson.SnakeCase);
        return reply!.SessionId;
    }

    /// <summary>The toolsets this API can load: the ones whose settings are set.</summary>
    public async Task<string[]> AvailableToolsetsAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "v1/toolsets"));
        var reply = await response.Content.ReadFromJsonAsync<ToolsetsReply>(InfoJson.SnakeCase);
        return reply!.Toolsets.Where(t => t.Available).Select(t => t.Name).ToArray();
    }

    /// <summary>Stops the session's turn, if one is running.</summary>
    public async Task CancelAsync(string sessionId)
    {
        using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{sessionId}/cancel"));
    }

    /// <summary>Ends the session on the server; one the server no longer has is fine.</summary>
    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"v1/sessions/{sessionId}"));
        }
        catch (AgentApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            // Already gone on the server; deleting the local copy is all that's left.
        }
    }

    /// <summary>The exchange rates the API loaded (F14).</summary>
    public Task<RatesTable> GetRatesAsync() => GetAsync<RatesTable>("v1/rates");

    /// <summary>The server's display currency and a symbol for each currency (F14).</summary>
    public Task<RatesDisplay> GetRatesDisplayAsync() => GetAsync<RatesDisplay>("v1/rates/display");

    /// <summary>Reads JSON from one of an app's own routes (the code-review app's workspace and git).</summary>
    public async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Sends a message and hands back the answer's events as they arrive.</summary>
    public async IAsyncEnumerable<SessionEvent> StreamMessageAsync(
        string sessionId, string message, IReadOnlyList<Attachment> attachments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // The browser buffers a reply unless told to stream it, which would hold the whole turn back until its end.
        var request = new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{sessionId}/messages")
        {
            Content = JsonContent.Create(new MessageBody(message, attachments)),
        };
        request.SetBrowserResponseStreamingEnabled(true);

        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, cancellationToken))
            yield return streamEvent;
    }

    /// <summary>Reads plain text: the admin portal's saved stream events (F27).</summary>
    public async Task<string> GetTextAsync(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Reads a file's bytes, such as the people's labels as a zip (F18 step 6a.3): through the client, because it
    /// carries the admin key and a plain link can't.
    /// </summary>
    public async Task<byte[]> GetBytesAsync(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>Posts to one of an app's own routes and hands back its JSON (the admin portal's runs, F27).</summary>
    public async Task<JsonElement> PostAsync(string url, object body)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        });
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Follows a stream of server-sent events whose data is plain text, such as the admin portal's run lines (F27):
    /// each event's type and data.
    /// </summary>
    public async IAsyncEnumerable<(string Type, string Data)> StreamLinesAsync(string url,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.SetBrowserResponseStreamingEnabled(true);
        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
            yield return (item.EventType, item.Data);
    }

    /// <summary>
    /// Sends with the right key. If the API says no, it throws <see cref="AgentApiException"/> with the API's own
    /// words.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default)
    {
        // The admin portal's requests carry its own key, the chat's carry the chat's (F31).
        if (settings.KeyFor(request.RequestUri) is { } key && !string.IsNullOrWhiteSpace(key))
            request.Headers.Add("x-api-key", key);
        var response = await http.SendAsync(request, completion, cancellationToken);
        if (response.IsSuccessStatusCode) return response;

        string message = await ErrorMessageAsync(response, cancellationToken);
        var status = response.StatusCode;
        response.Dispose();
        throw new AgentApiException(status, message);
    }

    /// <summary>
    /// The words the API sent with its error, or the status text when the reply isn't the API's error.
    /// </summary>
    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string fallback = response.ReasonPhrase ?? response.StatusCode.ToString();
        // A proxy's page or an empty body isn't the API's error shape; the status text will do then.
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonSerializerOptions.Web,
                cancellationToken);
            return error?.Error?.Message ?? fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
