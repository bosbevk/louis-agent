using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Sessions;

/// <summary>
/// FX207, Louis's pick of three ("mark talks while you're signed in"): a browser that holds the admin key says a
/// session it opened is the admin's, behind that key, so the overview can tell the admin's talks from visitors'.
/// </summary>
public class MineRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>With the key, the session is marked the admin's; one nobody marked stays a visitor's.</summary>
    [Test]
    public async Task WithTheKey_TheSessionIsMarkedTheAdmins()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();
        string mine = await OpenAsync(app);
        string theirs = await OpenAsync(app);

        using var response = await PostAsync(app, $"/v1/admin/sessions/{mine}/mine", AdminKey);
        var sessions = app.Services.GetRequiredService<AgentSessions>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(sessions.Get(mine)!.FromTheAdmin, Is.True);
            Assert.That(sessions.Get(theirs)!.FromTheAdmin, Is.False);
        });
    }

    /// <summary>Without the key nothing is marked, and a session that doesn't exist is a 404.</summary>
    [Test]
    public async Task AWrongKeyOrAnUnknownSession_IsRefused()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();
        string id = await OpenAsync(app);

        using var wrongKey = await PostAsync(app, $"/v1/admin/sessions/{id}/mine", "wrong");
        using var unknown = await PostAsync(app, "/v1/admin/sessions/sess_none/mine", AdminKey);

        Assert.Multiple(() =>
        {
            Assert.That(wrongKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(app.Services.GetRequiredService<AgentSessions>().Get(id)!.FromTheAdmin, Is.False);
            Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>Opens a session and gives back its id.</summary>
    private static async Task<string> OpenAsync(WebApplication app)
    {
        var body = new Dictionary<string, string>();
        using var opened = await app.GetTestClient().PostAsJsonAsync("/v1/sessions", body);
        return JsonDocument.Parse(await opened.Content.ReadAsStringAsync()).RootElement.GetProperty("session_id")
            .GetString()!;
    }

    /// <summary>Asks the admin route with this key.</summary>
    private static Task<HttpResponseMessage> PostAsync(WebApplication app, string path, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("x-api-key", key);
        return app.GetTestClient().SendAsync(request);
    }
}
