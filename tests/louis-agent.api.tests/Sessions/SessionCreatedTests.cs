using LouisAgent.Core.Agent.Toolsets;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Sessions;

// CL20 step 5 (ADR 0039, rule 3): a create answers 201 and names what it made in Location.
public class SessionCreatedTests
{
    [Test]
    public async Task OpeningASession_Answers201_WithItsLocation()
    {
        await using var app = ApiTestHost.Build();
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/v1/sessions", new { toolsets = new[] { CoreToolset.Files.Name() } });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("session_id").GetString()!;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo($"/v1/sessions/{id}"));
        using var read = await client.GetAsync(response.Headers.Location);
        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
