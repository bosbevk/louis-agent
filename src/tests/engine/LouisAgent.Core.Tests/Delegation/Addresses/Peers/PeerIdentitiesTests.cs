using System.Net;
using System.Text;
using LouisAgent.Core.Delegation.Addresses.Peers;
using LouisAgent.Core.Delegation.SubSessions;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Delegation.Addresses.Peers;

/// <summary>
/// A peer's identities (F127.3.2): before an address on another server is trusted, that server's
/// <c>GET /v1/identities</c> is read with its own key and kept ten minutes (Claude's pick Q2); an identity it doesn't
/// list, a key it refuses and a server that doesn't answer each come back as a reason the model can pass on.
/// </summary>
public class PeerIdentitiesTests
{
    /// <summary>The partner's server.</summary>
    private static readonly Uri Partner = new("https://partner.example/");

    /// <summary>The identities the partner lists, as our API writes them.</summary>
    private const string Listed =
        """{"identities":[{"name":"shop","voice":"shop/voice-en","packs":[],"default":true},{"name":"cafe","voice":null,"packs":[],"default":false}]}""";

    /// <summary>The partner's server, answering every request this way.</summary>
    private static StubHandler Server(Func<HttpResponseMessage> answer) => new(_ => answer());

    /// <summary>A reply with this status and body.</summary>
    private static HttpResponseMessage Reply(HttpStatusCode status, string body = "") =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>The directory over this server, its clock as given.</summary>
    private static PeerIdentities Directory(StubHandler server, TimeProvider? time = null) =>
        new(new PeerHttp(new HttpClient(server), "own-key"), time ?? TimeProvider.System);

    /// <summary>An identity the partner lists is no reason to stop, and the list was asked for with its key.</summary>
    [Test]
    public async Task AListedIdentity_PassesTheCheck()
    {
        var server = Server(() => Reply(HttpStatusCode.OK, Listed));

        string? why = await Directory(server).RefusalAsync("partner", Partner, "shop");

        Assert.Multiple(() =>
        {
            Assert.That(why, Is.Null);
            Assert.That(server.Requests.Single().Url, Is.EqualTo("https://partner.example/v1/identities"));
        });
    }

    /// <summary>An identity it doesn't list is refused, naming the ones it does.</summary>
    [Test]
    public async Task AnIdentityItDoesntList_IsRefused_NamingTheOnesItDoes() =>
        Assert.That(await Directory(Server(() => Reply(HttpStatusCode.OK, Listed))).RefusalAsync("partner", Partner,
                "bakery"),
            Does.StartWith("Error").And.Contain("bakery").And.Contain("cafe, shop"));

    /// <summary>A key the partner refuses says so, and asks nothing.</summary>
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    public async Task ARefusedKey_SaysSo(HttpStatusCode status) =>
        Assert.That(await Directory(Server(() => Reply(status))).RefusalAsync("partner", Partner, "shop"),
            Does.StartWith("Error").And.Contain("partner").And.Contain("key"));

    /// <summary>A server that doesn't answer, or answers with an error, says it couldn't be reached.</summary>
    [Test]
    public async Task AServerThatIsDown_SaysSo()
    {
        var down = Server(() => throw new HttpRequestException("connection refused"));
        var failing = Server(() => Reply(HttpStatusCode.BadGateway));

        Assert.Multiple(async () =>
        {
            Assert.That(await Directory(down).RefusalAsync("partner", Partner, "shop"),
                Does.StartWith("Error").And.Contain("partner").And.Contain("didn't answer"));
            Assert.That(await Directory(failing).RefusalAsync("partner", Partner, "shop"),
                Does.StartWith("Error").And.Contain("didn't answer"));
        });
    }

    /// <summary>The list is kept ten minutes: asked again within them, the server isn't; after them, it is.</summary>
    [Test]
    public async Task TheList_IsKeptTenMinutes()
    {
        var server = Server(() => Reply(HttpStatusCode.OK, Listed));
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-11-12T10:00:00Z"));
        var directory = Directory(server, time);

        await directory.RefusalAsync("partner", Partner, "shop");
        time.Advance(TimeSpan.FromMinutes(9));
        await directory.RefusalAsync("partner", Partner, "cafe");
        int withinTen = server.Requests.Count;
        time.Advance(TimeSpan.FromMinutes(2));
        await directory.RefusalAsync("partner", Partner, "shop");

        Assert.That((withinTen, server.Requests.Count), Is.EqualTo((1, 2)));
    }

    /// <summary>A failure isn't kept: the next ask tries the server again.</summary>
    [Test]
    public async Task AFailure_IsNotKept()
    {
        int calls = 0;
        var server = Server(() => ++calls == 1 ? Reply(HttpStatusCode.BadGateway) : Reply(HttpStatusCode.OK, Listed));
        var directory = Directory(server);

        await directory.RefusalAsync("partner", Partner, "shop");

        Assert.That(await directory.RefusalAsync("partner", Partner, "shop"), Is.Null);
    }
}
