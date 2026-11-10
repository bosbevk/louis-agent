using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints;
using LouisAgent.Choices;
using LouisAgent.Identities.Puls;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The guide's door (F109.3, Louis's pick C1; a list since F127.2, Louis's pick "a list, one cap"): a visitor with no
/// key and no sign-in may open a session as one of the identities the host names open, with only that identity's own
/// toolsets, a few times a day from one address across them all; the calls on such a session pass; everything else
/// stays behind the key, and a shut door changes nothing.
/// </summary>
public class OpenDoorTests
{
    private const string Key = "test-key";

    /// <summary>An open identity: a second one beside Puls's, on an embedded voice, with no packs.</summary>
    private static readonly FakeIdentity Guide = new("guide", voice: "puls/voice-en");

    /// <summary>Another open identity, as a showroom's business is beside the guide.</summary>
    private static readonly FakeIdentity Shop = new("shop", voice: "puls/voice-en");

    /// <summary>Two conversations a day from one address, with the guide or the shop.</summary>
    private static readonly OpenDoorOptions Door = new(["guide", "shop"], 2);

    /// <summary>A host with Puls, the guide and the shop, behind the key, its door as given.</summary>
    private static Microsoft.AspNetCore.Builder.WebApplication Host(OpenDoorOptions door) =>
        ApiTestHost.Build(Key, moreIdentities: [Guide, Shop], openDoor: door);

    private static async Task<HttpResponseMessage> OpenAsync(HttpClient client, object body) =>
        await client.PostAsJsonAsync("/v1/sessions", body);

    /// <summary>
    /// Each open identity's session opens with no key, in that identity and no other, and its own calls pass.
    /// </summary>
    [TestCase("guide")]
    [TestCase("shop")]
    public async Task AVisitor_OpensAnOpenIdentity_AndItsCallsPass(string identity)
    {
        await using var app = Host(Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new { identity });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("session_id").GetString()!;
        using var read = await client.GetAsync($"/v1/sessions/{id}");
        using var closed = await client.DeleteAsync($"/v1/sessions/{id}");

        Assert.Multiple(() =>
        {
            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(body.RootElement.GetProperty("identity").GetString(), Is.EqualTo(identity));
            Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(closed.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    /// <summary>
    /// The body's toolsets are not the visitor's to choose: the session gets the identity's own, nothing more.
    /// </summary>
    [Test]
    public async Task AnOpenSession_GetsOnlyTheIdentitysToolsets()
    {
        await using var app = Host(Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new
        {
            identity = "shop",
            toolsets = new[] { TestToolset.Files.Wire(), TestToolset.Web.Wire() }
        });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());

        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body.RootElement.GetProperty("toolsets").EnumerateArray(), Is.Empty);
    }

    /// <summary>Any identity the door doesn't name, or none, is behind the key as before.</summary>
    [Test]
    public async Task AnotherIdentity_StaysBehindTheKey()
    {
        await using var app = Host(Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        foreach (string? identity in new[] { PulsIdentityName.Puls.Wire(), null })
        {
            using var opened = await OpenAsync(client, new { identity });
            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), identity ?? "(none)");
        }
    }

    /// <summary>
    /// The day's cap is one across every open identity (Louis: "the guide's door only"): past it the door says so with
    /// a 429, whichever identity is asked for, and a session of someone else's is still not reachable.
    /// </summary>
    [Test]
    public async Task TheCap_CountsAcrossTheOpenIdentities()
    {
        await using var app = Host(Door);
        await app.StartAsync();
        var client = app.GetTestClient();
        using var keyed = new HttpRequestMessage(HttpMethod.Post, "/v1/sessions");
        keyed.Content = JsonContent.Create(new { });
        keyed.Headers.Add("x-api-key", Key);
        using var theirs = await client.SendAsync(keyed);
        string theirId = JsonDocument.Parse(await theirs.Content.ReadAsStringAsync()).RootElement
            .GetProperty("session_id").GetString()!;

        using var first = await OpenAsync(client, new { identity = "guide" });
        using var second = await OpenAsync(client, new { identity = "shop" });
        using var third = await OpenAsync(client, new { identity = "guide" });
        string text = await third.Content.ReadAsStringAsync();
        using var peek = await client.GetAsync($"/v1/sessions/{theirId}");

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(third.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(text, Does.Contain(ApiErrorKind.RateLimit.Wire()));
            Assert.That(peek.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "a keyed session is not the door's");
        });
    }

    /// <summary>
    /// A shut door (no identity named, or no cap) changes nothing: the guide is behind the key like the rest.
    /// </summary>
    [TestCase(new[] { "guide" }, 0)]
    [TestCase(new string[0], 2)]
    public async Task AShutDoor_ChangesNothing(string[] identities, int cap)
    {
        await using var app = Host(new OpenDoorOptions(identities, cap));
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new { identity = "guide" });

        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// The settings: the identities' names, by commas, and a whole number of sessions; anything else keeps the door
    /// shut.
    /// </summary>
    [TestCase("louis-agent-guide, seemeeu-guesthouse", "20", new[] { "louis-agent-guide", "seemeeu-guesthouse" }, 20,
        true)]
    [TestCase("louis-agent-guide", "lots", new[] { "louis-agent-guide" }, 0, false)]
    [TestCase(" , ", "20", new string[0], 20, false)]
    [TestCase(null, null, new string[0], 0, false)]
    public void TheSettings_NameTheIdentitiesAndTheCap(string? identities, string? cap, string[] expected,
        int expectedCap, bool open)
    {
        var options = OpenDoorOptions.FromEnvironment(name => name switch
        {
            "OPEN_IDENTITIES" => identities,
            "OPEN_SESSIONS_PER_DAY" => cap,
            _ => null
        });

        Assert.Multiple(() =>
        {
            Assert.That(options.Identities, Is.EqualTo(expected));
            Assert.That(options.SessionsPerDay, Is.EqualTo(expectedCap));
            Assert.That(options.IsOpen, Is.EqualTo(open));
            Assert.That(options.Identities, Does.Not.Contain(PulsIdentityName.Puls.Wire()), "the guide is never Puls");
        });
    }

    /// <summary>The door admits a named identity whatever its case, and names it as the host does.</summary>
    [TestCase("guide", "guide")]
    [TestCase("SHOP", "shop")]
    [TestCase("nobody", null)]
    [TestCase(null, null)]
    public void TheDoor_AdmitsTheIdentitiesItNames(string? asked, string? admitted) =>
        Assert.That(Door.Admits(asked), Is.EqualTo(admitted));
}
