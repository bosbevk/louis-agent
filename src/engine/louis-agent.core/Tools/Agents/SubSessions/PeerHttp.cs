namespace LouisAgent.Core.Tools.Agents.SubSessions;

using System.Net.Http.Json;
using System.Text.Json;

/// <summary>The one way a request reaches a peer's API (S, CL15, from SpecialistTools): the key from AGENT_API_KEY, never from the model's arguments.</summary>
internal sealed class PeerHttp(HttpClient http, string? apiKey)
{
    /// <summary>One client for every engine: sub-session answers can take minutes, so the timeout is the tools', not HttpClient's.</summary>
    public static readonly HttpClient Shared = new() { Timeout = Timeout.InfiniteTimeSpan };

    public HttpRequestMessage Request(HttpMethod method, Uri uri, object? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-api-key", apiKey);
        return request;
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        http.SendAsync(request, cancellationToken);

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completion, CancellationToken cancellationToken) =>
        http.SendAsync(request, completion, cancellationToken);

    /// <summary>The message in an API error body, else the body itself, cut short.</summary>
    public static string ErrorText(string body)
    {
        try { return JsonDocument.Parse(body).RootElement.GetProperty("error").GetProperty("message").GetString() ?? body; }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return body.Length > 300 ? body[..300] : body; }
    }
}
