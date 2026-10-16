using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LouisAgent.Web.Shared.Services;

/// <summary>Thrown for an API error response; <see cref="Status"/> lets the UI react (401 = key, 404 = session gone).</summary>
public sealed class AgentApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>One streamed event: its type (content_block_delta, message_stop, ...) and its JSON data.</summary>
public sealed record AgentStreamEvent(string Type, JsonElement Data);

/// <summary>Client for louis-agent.api, which serves the web app from the same origin.</summary>
public sealed class AgentApi(HttpClient http, Settings settings)
{
    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (F5): a session's tools are chosen when it opens and kept. Each app
    /// decides which: the code-review app every available toolset, Puls only <c>agents</c> (it delegates).
    /// </summary>
    public async Task<string> CreateSessionAsync(IEnumerable<string> toolsets, string? personality = null,
        string? location = null, string? timeZone = null)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "sessions")
        {
            // personality (F21): the session's voice; null is the API's default voice (CL9). location and time_zone
            // (F22): where the user is, only when they shared it.
            Content = JsonContent.Create(new { toolsets, personality, location, time_zone = timeZone }),
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("session_id").GetString()!;
    }

    /// <summary>The toolsets this API can load (<c>GET /toolsets</c>, those whose settings are set).</summary>
    public async Task<string[]> AvailableToolsetsAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "toolsets"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("toolsets").EnumerateArray()
            .Where(t => t.GetProperty("available").GetBoolean())
            .Select(t => t.GetProperty("name").GetString()!)
            .ToArray();
    }

    public async Task CancelAsync(string sessionId)
    {
        using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"sessions/{sessionId}/cancel"));
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"sessions/{sessionId}"));
        }
        catch (AgentApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            // Already gone on the server; deleting the local copy is all that's left.
        }
    }

    /// <summary>The exchange rates the API loaded (F14).</summary>
    public Task<RatesTable> GetRatesAsync() => GetAsync<RatesTable>("rates");

    /// <summary>The server's display currency and a symbol for each currency (F14).</summary>
    public Task<RatesDisplay> GetRatesDisplayAsync() => GetAsync<RatesDisplay>("rates/display");

    /// <summary>A GET returning JSON, for an app's own endpoints (the code-review app's workspace and git).</summary>
    public async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Sends a message and yields the answer's events as they arrive.</summary>
    public async IAsyncEnumerable<AgentStreamEvent> StreamMessageAsync(
        string sessionId, string message, IReadOnlyList<Attachment> attachments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"sessions/{sessionId}/messages")
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
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
            yield return new AgentStreamEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement);
    }

    /// <summary>A GET returning plain text (the admin portal's saved stream events, F27).</summary>
    public async Task<string> GetTextAsync(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return await response.Content.ReadAsStringAsync();
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
        if (!string.IsNullOrWhiteSpace(settings.ApiKey)) request.Headers.Add("x-api-key", settings.ApiKey);
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
