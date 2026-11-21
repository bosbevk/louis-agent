using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.SubSessions;

namespace LouisAgent.Core.Delegation.Addresses.Peers;

/// <summary>
/// Another server's identities (F127.3.2): before an address on it is trusted, its <c>GET /v1/identities</c> is read
/// with its own key and kept ten minutes, so an agent it doesn't have is refused before a session is opened there, and
/// a refused key or a server that doesn't answer is said plainly.
/// </summary>
/// <remarks>
/// A Cache-Aside (Microsoft's cloud patterns): the list is read when it isn't kept, then kept a while; only a list is
/// kept, never a failure, so a server back up is asked again at once. Ten minutes, of three (read on every ask, a round
/// trip each time; a list by hand in a setting, which drifts from the other side): the other side changes its agents
/// rarely, and a new one is seen within minutes. The reply is read through a record of its own, since core can't see
/// the API's contracts: the names our API writes, held by the two-server test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
internal sealed class PeerIdentities(PeerHttp http, TimeProvider time)
{
    /// <summary>How long a server's list is kept.</summary>
    public static readonly TimeSpan Kept = TimeSpan.FromMinutes(10);

    /// <summary>How long a server gets to say who it has.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>The names in a reply, whatever case it writes them in.</summary>
    private static readonly JsonSerializerOptions ReplyJson = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<Uri, Known> _kept = new();

    /// <summary>Why an address on this server can't be asked; nothing when the server lists the identity.</summary>
    /// <remarks>Guard clauses (Fowler): the list, then whether it names the identity.</remarks>
    public async Task<string?> RefusalAsync(string peer, Uri url, string identity,
        CancellationToken cancellationToken = default)
    {
        var (names, why) = await NamesAsync(peer, url, cancellationToken);
        if (names is null) return why;
        if (names.Contains(identity, StringComparer.OrdinalIgnoreCase)) return null;
        return MessageText.Format(SubSessionToolMessage.PeerUnknownIdentity,
            new
            {
                Peer = peer,
                Identity = identity,
                Identities = names.Count == 0 ? "(none)" : string.Join(", ", names.Order(StringComparer.Ordinal))
            });
    }

    /// <summary>The server's identities, kept or read now; none and the reason when it can't say.</summary>
    private async Task<(IReadOnlyList<string>? Names, string? Why)> NamesAsync(string peer, Uri url,
        CancellationToken cancellationToken)
    {
        if (_kept.TryGetValue(url, out var known) && time.GetUtcNow() - known.At < Kept) return (known.Names, null);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);
        using var request = http.Request(HttpMethod.Get, new Uri(url, "v1/identities"), null);
        // Any way the server can't say is a reason for the model, never a crash of the turn.
        try
        {
            using var response = await http.SendAsync(request, deadline.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, MessageText.Format(SubSessionToolMessage.PeerKeyRefused, new { Peer = peer }));
            if (!response.IsSuccessStatusCode)
                return (null, Unreachable(peer, $"{(int)response.StatusCode} {response.ReasonPhrase}"));
            var reply = await response.Content.ReadFromJsonAsync<IdentitiesReply>(ReplyJson, deadline.Token);
            var names = reply?.Identities.Select(row => row.Name).ToList() ?? [];
            _kept[url] = new Known(time.GetUtcNow(), names);
            return (names, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            return (null, Unreachable(peer, ex.Message));
        }
    }

    /// <summary>The reason a server that didn't answer gives.</summary>
    private static string Unreachable(string peer, string reason) =>
        MessageText.Format(SubSessionToolMessage.PeerUnreachable,
            new
            {
                Peer = peer,
                Reason = reason
            });

    /// <summary>A server's list, and when it was read.</summary>
    private sealed record Known(DateTimeOffset At, IReadOnlyList<string> Names);

    /// <summary>The part of <c>GET /v1/identities</c>' reply this reads.</summary>
    private sealed record IdentitiesReply(IReadOnlyList<IdentityRow> Identities);

    /// <summary>One identity in the reply: its name is all this needs.</summary>
    private sealed record IdentityRow(string Name);
}
