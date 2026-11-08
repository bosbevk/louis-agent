using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Identities.Puls;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A session names its identity (F109.3, Louis's pick A2): a host runs the identities its catalogue holds, a session
/// opens as one of them and gets that identity's voice unless it names a role of its own, the reply says who, an
/// unknown name is refused, and the host lists who it runs.
/// </summary>
public class SessionIdentityTests
{
    /// <summary>
    /// A second identity beside Puls's. Its voice is one of Puls's modes: the prompt source is process-wide and frozen
    /// after its first read, so a test can't add a voice file of its own; naming an embedded one exercises the same path.
    /// </summary>
    private static readonly FakeIdentity Guide = new("guide", voice: "puls/voice-en");

    /// <summary>Naming the second identity opens the session in its voice, and the reply says so.</summary>
    [Test]
    public async Task ASession_NamingAnIdentity_OpensInItsVoice()
    {
        await using var app = ApiTestHost.Build(moreIdentities: [Guide]);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/v1/sessions", new { identity = "guide" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(body.RootElement.GetProperty("identity").GetString(), Is.EqualTo("guide"));
            Assert.That(body.RootElement.GetProperty("role").GetString(), Is.EqualTo("puls/voice-en"));
        });
    }

    /// <summary>A session that names none is the host's first identity, in its default voice.</summary>
    [Test]
    public async Task ASession_NamingNoIdentity_IsTheHostsFirst()
    {
        await using var app = ApiTestHost.Build(moreIdentities: [Guide]);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/v1/sessions", new { });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(body.RootElement.GetProperty("identity").GetString(), Is.EqualTo(PulsIdentityName.Puls.Wire()));
    }

    /// <summary>An identity the host doesn't run is a 400 that names what it runs.</summary>
    [Test]
    public async Task AnUnknownIdentity_IsRefused()
    {
        await using var app = ApiTestHost.Build(moreIdentities: [Guide]);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/v1/sessions", new { identity = "nobody" });
        string text = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(text, Does.Contain("nobody").And.Contain("guide"));
    }

    /// <summary>The host lists the identities it runs, each with its voice and its packs, the first as the default.</summary>
    [Test]
    public async Task TheHost_ListsTheIdentitiesItRuns()
    {
        await using var app = ApiTestHost.Build(moreIdentities: [Guide]);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.GetAsync("/v1/identities");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.GetProperty("identities").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(rows.Select(row => row.GetProperty("name").GetString()),
                Is.EqualTo(new[] { PulsIdentityName.Puls.Wire(), "guide" }));
            Assert.That(rows[1].GetProperty("voice").GetString(), Is.EqualTo("puls/voice-en"));
            Assert.That(rows[0].GetProperty("default").GetBoolean(), Is.True);
            Assert.That(rows[1].GetProperty("default").GetBoolean(), Is.False);
        });
    }
}
