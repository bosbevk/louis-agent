using System.Net;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// CL27: the admin portal lists every built-in prompt by what its header says: its path, name, description, level and
/// the toolsets it always loads. Behind the admin key, like every admin route.
/// </summary>
public class AdminPromptsTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>Each prompt comes with its header's fields; a guide or the principles name no level.</summary>
    [Test]
    public async Task ThePrompts_AreListed_EachWithItsHeader()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var response = await GetAsync(app, "/v1/admin/prompts", AdminKey);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var rows = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("prompts")
            .EnumerateArray().ToDictionary(row => row.GetProperty("path").GetString()!);
        var voice = rows["agent/puls/voice.md"];
        var principles = rows["shared/principles.md"];
        Assert.That((voice.GetProperty("name").GetString(), voice.GetProperty("level").GetString()),
            Is.EqualTo(("voice", RoleLevel.Agent.Wire())));
        Assert.That(voice.GetProperty("description").GetString(), Is.Not.Empty);
        Assert.That((principles.GetProperty("level").ValueKind, principles.GetProperty("toolsets").GetArrayLength()),
            Is.EqualTo((JsonValueKind.Null, 0)));
    }

    /// <summary>Without the admin key the list is refused.</summary>
    [Test]
    public async Task WithoutTheKey_ThePromptsAreRefused()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var response = await GetAsync(app, "/v1/admin/prompts", "wrong");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Asks the admin route with this key.</summary>
    private static Task<HttpResponseMessage> GetAsync(WebApplication app, string path, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", key);
        return app.GetTestClient().SendAsync(request);
    }
}
