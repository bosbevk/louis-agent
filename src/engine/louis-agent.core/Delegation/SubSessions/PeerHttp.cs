using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// The one way a request reaches a peer's API (S, CL15): the key from AGENT_API_KEY, never from the model's arguments.
/// </summary>
internal sealed class PeerHttp(HttpClient http, string? apiKey)
{
    /// <summary>
    /// One client for every engine: sub-session answers can take minutes, so the timeout is the tools', not
    /// HttpClient's.
    /// </summary>
    public static readonly HttpClient Shared = new() { Timeout = Timeout.InfiniteTimeSpan };

    /// <summary>How long an asked agent may take to answer, unless a host says otherwise.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Finds the peer named, or with none named and only one there, that one, so the model never has to guess a name;
    /// otherwise the refusal that lists them.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per case, instead of an <c>if</c> inside an <c>if</c>.</remarks>
    public static string? ResolvePeer(IReadOnlyDictionary<string, Uri> peers, string? agent, out Uri peer,
        out string name)
    {
        peer = null!;
        name = agent?.Trim() ?? "";
        string peerNames = peers.Count == 0 ? "(none)" : string.Join(", ", peers.Keys.Order());
        if (name.Length == 0 && peers.Count != 1)
            return MessageText.Format(SubSessionToolMessage.PeerNameNeeded, new { Peers = peerNames });
        if (name.Length == 0) name = peers.Keys.Single();
        if (!peers.TryGetValue(name, out Uri? found))
            return MessageText.Format(SubSessionToolMessage.UnknownPeer, new
            {
                Agent = name,
                Peers = peerNames
            });

        peer = found;
        return null;
    }

    public HttpRequestMessage Request(HttpMethod method, Uri uri, object? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-api-key", apiKey);
        return request;
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        http.SendAsync(request, cancellationToken);

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completion,
        CancellationToken cancellationToken) =>
        http.SendAsync(request, completion, cancellationToken);

    /// <summary>The message in an API error body, else the body itself, cut short.</summary>
    public static string ErrorText(string body)
    {
        try
        {
            return JsonDocument.Parse(body).RootElement.GetProperty("error").GetProperty("message").GetString() ?? body;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return body.Length > 300 ? body[..300] : body;
        }
    }
}
