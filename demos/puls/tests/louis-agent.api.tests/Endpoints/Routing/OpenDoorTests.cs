using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Choices;
using LouisAgent.Identities.Puls;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The guide's door (F109.3, Louis's pick C1): a visitor with no key and no sign-in may open a session as the one
/// identity the host names open, with only that identity's own toolsets, a few times a day from one address; the
/// calls on such a session pass; everything else stays behind the key, and a shut door changes nothing.
/// </summary>
public class OpenDoorTests
{
    private const string Key = "test-key";

    /// <summary>The open identity: a second one beside Puls's, on an embedded voice, with no packs.</summary>
    private static readonly FakeIdentity Guide = new("guide", voice: "puls/voice-en");

    /// <summary>Two conversations a day from one address.</summary>
    private static readonly OpenDoorOptions Door = new("guide", 2);

    private static async Task<HttpResponseMessage> OpenAsync(HttpClient client, object body) =>
        await client.PostAsJsonAsync("/v1/sessions", body);

    /// <summary>The open identity's session opens with no key, in that identity, and its own calls pass after.</summary>
    [Test]
    public async Task AVisitor_OpensTheGuide_AndItsCallsPass()
    {
        await using var app = ApiTestHost.Build(Key, moreIdentities: [Guide], openDoor: Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new { identity = "guide" });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("session_id").GetString()!;
        using var read = await client.GetAsync($"/v1/sessions/{id}");
        using var closed = await client.DeleteAsync($"/v1/sessions/{id}");

        Assert.Multiple(() =>
        {
            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(body.RootElement.GetProperty("identity").GetString(), Is.EqualTo("guide"));
            Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(closed.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    /// <summary>The body's toolsets are not the visitor's to choose: the session gets the identity's own, nothing more.</summary>
    [Test]
    public async Task TheGuidesSession_GetsOnlyTheIdentitysToolsets()
    {
        await using var app = ApiTestHost.Build(Key, moreIdentities: [Guide], openDoor: Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new
        {
            identity = "guide",
            toolsets = new[] { "files", "web" }
        });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());

        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body.RootElement.GetProperty("toolsets").EnumerateArray(), Is.Empty);
    }

    /// <summary>Any other identity, or none, is behind the key as before.</summary>
    [TestCase("puls")]
    [TestCase(null)]
    public async Task AnotherIdentity_StaysBehindTheKey(string? identity)
    {
        await using var app = ApiTestHost.Build(Key, moreIdentities: [Guide], openDoor: Door);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new { identity });

        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Past the day's cap the door says so with a 429, and a session of someone else's is still not reachable.</summary>
    [Test]
    public async Task PastTheCap_TheDoorSays429_AndAnotherSessionStaysClosed()
    {
        await using var app = ApiTestHost.Build(Key, moreIdentities: [Guide], openDoor: Door);
        await app.StartAsync();
        var client = app.GetTestClient();
        using var keyed =
            new HttpRequestMessage(HttpMethod.Post, "/v1/sessions") { Content = JsonContent.Create(new { }) };
        keyed.Headers.Add("x-api-key", Key);
        using var theirs = await client.SendAsync(keyed);
        string theirId = JsonDocument.Parse(await theirs.Content.ReadAsStringAsync()).RootElement
            .GetProperty("session_id").GetString()!;

        using var first = await OpenAsync(client, new { identity = "guide" });
        using var second = await OpenAsync(client, new { identity = "guide" });
        using var third = await OpenAsync(client, new { identity = "guide" });
        string text = await third.Content.ReadAsStringAsync();
        using var peek = await client.GetAsync($"/v1/sessions/{theirId}");

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(third.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(text, Does.Contain("rate_limit_error"));
            Assert.That(peek.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "a keyed session is not the door's");
        });
    }

    /// <summary>A shut door (no identity named, or no cap) changes nothing: the guide is behind the key like the rest.</summary>
    [Test]
    public async Task AShutDoor_ChangesNothing()
    {
        await using var app =
            ApiTestHost.Build(Key, moreIdentities: [Guide], openDoor: new OpenDoorOptions("guide", 0));
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened = await OpenAsync(client, new { identity = "guide" });

        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>The settings: the identity's name and a whole number of sessions; anything else keeps the door shut.</summary>
    [TestCase("louis-agent-expert", "20", "louis-agent-expert", 20, true)]
    [TestCase("louis-agent-expert", "lots", "louis-agent-expert", 0, false)]
    [TestCase("", "20", null, 20, false)]
    [TestCase(null, null, null, 0, false)]
    public void TheSettings_NameTheIdentityAndTheCap(string? identity, string? cap, string? expectedIdentity,
        int expectedCap, bool open)
    {
        var options = OpenDoorOptions.FromEnvironment(name => name switch
        {
            "OPEN_IDENTITY" => identity,
            "OPEN_SESSIONS_PER_DAY" => cap,
            _ => null
        });

        Assert.Multiple(() =>
        {
            Assert.That(options.Identity, Is.EqualTo(expectedIdentity));
            Assert.That(options.SessionsPerDay, Is.EqualTo(expectedCap));
            Assert.That(options.IsOpen, Is.EqualTo(open));
            Assert.That(PulsIdentityName.Puls.Wire(), Is.Not.EqualTo(expectedIdentity), "the guide is never Puls");
        });
    }
}
