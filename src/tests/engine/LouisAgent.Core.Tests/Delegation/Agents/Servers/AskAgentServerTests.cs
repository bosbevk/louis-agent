using System.Net;
using System.Text.Json;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Agents.Servers;

/// <summary>
/// An ask to an agent on another server (F127.3.2): an address that names its server goes there, with that server's
/// own key, once the server's identities list the agent; one that names none stays on this instance; an unknown
/// server or an agent it doesn't list is a reason, and nothing is opened.
/// </summary>
public class AskAgentServerTests
{
    /// <summary>This instance and a partner's server.</summary>
    private static readonly Dictionary<string, Uri> Servers = new()
    {
        ["self"] = new Uri("http://self.test/"),
        ["partner"] = new Uri("http://partner.test/")
    };

    /// <summary>The partner's own key.</summary>
    private static readonly Dictionary<string, string> Keys = new() { ["partner"] = "partner-key" };

    /// <summary>
    /// The asker, who may ask testprod here and the partner's shop, bakery and an unknown server's shop.
    /// </summary>
    private static readonly FakeIdentity Guide = new("guide", voice: "testprod/voice-en",
        calls: ["testprod", "partner:shop", "partner:bakery", "nowhere:shop"]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Guide, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>A request as it arrived: its method, its address and the key it carried.</summary>
    private sealed record Arrived(string Method, string Url, string? Key);

    /// <summary>
    /// Both servers in one handler: the partner lists the shop and the cafe; each opens a session, answers and lets it
    /// go.
    /// </summary>
    private static (StubHandler Handler, List<Arrived> Arrived) TwoServers()
    {
        var arrived = new List<Arrived>();
        var handler = new StubHandler(request =>
        {
            arrived.Add(new Arrived(request.Method.Method, request.RequestUri!.ToString(),
                request.Headers.TryGetValues("x-api-key", out var keys) ? keys.Single() : null));
            return request.RequestUri.AbsolutePath == "/v1/identities"
                ? Json(HttpStatusCode.OK, """{"identities":[{"name":"shop"},{"name":"cafe"}]}""")
                : Answer(request);
        });
        return (handler, arrived);
    }

    /// <summary>Asks as the guide, outside a chat.</summary>
    private static async Task<string> AskAsync(StubHandler handler, string address)
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Guide));
        return await new AskAgentTools(Servers, Identities, "own-key", new HttpClient(handler), peerKeys: Keys)
            .AskAgent(address, "Is a dog allowed?");
    }

    /// <summary>
    /// An address on the partner goes to the partner: its identities first, then the session, every request with the
    /// partner's own key, and the session opened as the shop.
    /// </summary>
    [Test]
    public async Task APeerAddress_GoesToThatServer_WithItsOwnKey()
    {
        var (handler, arrived) = TwoServers();

        string answer = await AskAsync(handler, "partner:shop");

        using var opening = JsonDocument.Parse(handler.Requests.Single(r => r.Url.EndsWith("/v1/sessions")).Body!);
        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Fixed the null check; 14/14 tests pass."));
            Assert.That(arrived.Select(a => a.Url), Has.All.StartWith("http://partner.test/"));
            Assert.That(arrived[0].Url, Is.EqualTo("http://partner.test/v1/identities"));
            Assert.That(arrived.Select(a => a.Key), Has.All.EqualTo("partner-key"));
            Assert.That(opening.RootElement.GetProperty("identity").GetString(), Is.EqualTo("shop"));
        });
    }

    /// <summary>An address with no server stays on this instance, with its own key, and reads no list.</summary>
    [Test]
    public async Task ALocalAddress_StaysOnThisInstance()
    {
        var (handler, arrived) = TwoServers();

        string answer = await AskAsync(handler, "testprod");

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Fixed the null check; 14/14 tests pass."));
            Assert.That(arrived.Select(a => a.Url), Does.Contain("http://self.test/v1/sessions"));
            Assert.That(arrived.Select(a => a.Url), Has.All.StartWith("http://self.test/"));
            Assert.That(arrived.Select(a => a.Key), Has.All.EqualTo("own-key"));
            Assert.That(arrived.Select(a => a.Url), Has.None.EndsWith("/v1/identities"));
        });
    }

    /// <summary>An agent the partner doesn't list is refused, naming the ones it does, and nothing is opened.</summary>
    [Test]
    public async Task AnAgentThePartnerDoesntList_IsRefused()
    {
        var (handler, arrived) = TwoServers();

        string answer = await AskAsync(handler, "partner:bakery");

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error").And.Contain("bakery").And.Contain("cafe, shop"));
            Assert.That(arrived.Select(a => a.Url), Is.EqualTo(new[] { "http://partner.test/v1/identities" }));
        });
    }

    /// <summary>
    /// A server this instance doesn't know is refused, naming the ones it does, and nothing is asked.
    /// </summary>
    [Test]
    public async Task AnUnknownServer_IsRefused()
    {
        var (handler, arrived) = TwoServers();

        string answer = await AskAsync(handler, "nowhere:shop");

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error").And.Contain("nowhere").And.Contain("partner"));
            Assert.That(arrived, Is.Empty);
        });
    }
}
