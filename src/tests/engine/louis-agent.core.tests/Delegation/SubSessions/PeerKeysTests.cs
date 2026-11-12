using LouisAgent.Core.Delegation.SubSessions;

namespace LouisAgent.Core.Tests.Delegation.SubSessions;

/// <summary>
/// A key per peer (F127.3.2, Claude's pick Q1): every request to a peer carries that peer's own key, and this
/// instance's own key when the peer has none, so one leaked key opens one server.
/// </summary>
public class PeerKeysTests
{
    /// <summary>The servers this instance knows.</summary>
    private static readonly Dictionary<string, Uri> Peers = new()
    {
        ["self"] = new Uri("http://localhost:8080/"),
        ["partner"] = new Uri("https://partner.example/api/")
    };

    /// <summary>The key a request to this address carries.</summary>
    private static string? KeyFor(string url)
    {
        var http = new PeerHttp(new HttpClient(), "own-key",
            PeerHttp.KeysByPeer(Peers, new Dictionary<string, string> { ["partner"] = "partner-key" }));
        using var request = http.Request(HttpMethod.Get, new Uri(url), null);
        return request.Headers.TryGetValues("x-api-key", out var keys) ? keys.Single() : null;
    }

    /// <summary>A request to a peer with its own key carries that key.</summary>
    [Test]
    public void ARequestToAPeerWithItsOwnKey_CarriesIt() =>
        Assert.That(KeyFor("https://partner.example/api/v1/identities"), Is.EqualTo("partner-key"));

    /// <summary>A request to a peer with no key of its own carries this instance's key, as before.</summary>
    [Test]
    public void ARequestToAPeerWithNoKey_CarriesThisInstancesKey() =>
        Assert.That(KeyFor("http://localhost:8080/v1/sessions"), Is.EqualTo("own-key"));

    /// <summary>A peer's key never goes to another server, even one whose address starts the same way.</summary>
    [Test]
    public void APeersKey_NeverGoesElsewhere() =>
        Assert.That(KeyFor("https://partner.example/apix/v1/sessions"), Is.EqualTo("own-key"));
}
