using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// The one way a request reaches a peer's API (S, CL15): the peer's own key from AGENT_PEER_KEYS (F127.3.2), else this
/// instance's from AGENT_API_KEY, never from the model's arguments.
/// </summary>
internal sealed class PeerHttp(HttpClient http, string? apiKey, IReadOnlyDictionary<Uri, string>? keys = null)
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

    /// <summary>A request to a peer, with the key of the peer it goes to.</summary>
    public HttpRequestMessage Request(HttpMethod method, Uri uri, object? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (KeyFor(uri) is { Length: > 0 } key) request.Headers.Add("x-api-key", key);
        return request;
    }

    /// <summary>
    /// Each peer's own key by its address, from the peers' names and the keys' names; a key for a peer not named has
    /// nowhere to go and is left out.
    /// </summary>
    public static IReadOnlyDictionary<Uri, string> KeysByPeer(IReadOnlyDictionary<string, Uri> peers,
        IReadOnlyDictionary<string, string> keys) =>
        peers.Where(peer => keys.ContainsKey(peer.Key)).ToDictionary(peer => peer.Value, peer => keys[peer.Key]);

    /// <summary>
    /// The key for a request: the peer's own, found by the address the request goes under (each peer's ends in a
    /// slash, so one server's key never matches another's that starts the same way), else this instance's.
    /// </summary>
    private string? KeyFor(Uri uri) =>
        keys?.FirstOrDefault(peer => uri.AbsoluteUri.StartsWith(peer.Key.AbsoluteUri, StringComparison.Ordinal))
            .Value ?? apiKey;

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
