using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.SessionStream;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LouisAgent.Web.Shared.Api;

using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using LouisAgent.Web.Shared.Storage;

/// <summary>Client for louis-agent.api, which serves the web app from the same origin.</summary>
public sealed class AgentApi(HttpClient http, ChatSettings settings)
{
    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (F5): a session's tools are chosen when it opens and kept. Each app
    /// decides which: the code-review app every available toolset, Puls only <c>specialists</c> (it delegates).
    /// </summary>
    public async Task<string> CreateSessionAsync(IEnumerable<string> toolsets, string? role = null,
        string? location = null, string? timeZone = null)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/sessions")
        {
            // role (F21; "personality" before CL14): the session's voice; null is the API's default voice (CL9). location
            // and time_zone (F22): where the user is, only when they shared it.
            Content = JsonContent.Create(new { toolsets, role, location, time_zone = timeZone }),
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("session_id").GetString()!;
    }

    /// <summary>The toolsets this API can load (<c>GET /v1/toolsets</c>, those whose settings are set).</summary>
    public async Task<string[]> AvailableToolsetsAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "v1/toolsets"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("toolsets").EnumerateArray()
            .Where(t => t.GetProperty("available").GetBoolean())
            .Select(t => t.GetProperty("name").GetString()!)
            .ToArray();
    }

    public async Task CancelAsync(string sessionId)
    {
        using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{sessionId}/cancel"));
    }

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

    /// <summary>A GET returning JSON, for an app's own endpoints (the code-review app's workspace and git).</summary>
    public async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Sends a message and yields the answer's events as they arrive.</summary>
    public async IAsyncEnumerable<SessionEvent> StreamMessageAsync(
        string sessionId, string message, IReadOnlyList<Attachment> attachments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"v1/sessions/{sessionId}/messages")
        {
            Content = JsonContent.Create(new
            {
                message,
                attachments = attachments.Select(a => new { name = a.Name, content = a.Content }),
            }),
        };
        request.SetBrowserResponseStreamingEnabled(true);

        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, cancellationToken))
            yield return streamEvent;
    }

    /// <summary>A GET returning plain text (the admin portal's saved stream events, F27).</summary>
    public async Task<string> GetTextAsync(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>A GET returning a file's bytes (the people's labels, a zip; F18 step 6a.3): through the client, which carries the admin key a plain link cannot.</summary>
    public async Task<byte[]> GetBytesAsync(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>A POST returning JSON, for an app's own endpoints (the admin portal's runs, F27).</summary>
    public async Task<JsonElement> PostAsync(string url, object body)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) });
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>A GET of server-sent events whose data is plain text (the admin portal's run lines, F27): each event's type and data.</summary>
    public async IAsyncEnumerable<(string Type, string Data)> StreamLinesAsync(string url, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.SetBrowserResponseStreamingEnabled(true);
        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
            yield return (item.EventType, item.Data);
    }

    /// <summary>Sends with the API key; throws <see cref="AgentApiException"/> with the API's message on an error.</summary>
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default)
    {
        // The admin portal's requests carry its own key, the chat's carry the chat's (F31).
        if (settings.KeyFor(request.RequestUri) is { } key && !string.IsNullOrWhiteSpace(key)) request.Headers.Add("x-api-key", key);
        var response = await http.SendAsync(request, completion, cancellationToken);
        if (response.IsSuccessStatusCode) return response;

        string message = response.ReasonPhrase ?? response.StatusCode.ToString();
        try
        {
            var error = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            message = error.GetProperty("error").GetProperty("message").GetString() ?? message;
        }
        catch (Exception)
        {
            // Not the API's JSON error shape; the status text will do.
        }

        var status = response.StatusCode;
        response.Dispose();
        throw new AgentApiException(status, message);
    }
}
