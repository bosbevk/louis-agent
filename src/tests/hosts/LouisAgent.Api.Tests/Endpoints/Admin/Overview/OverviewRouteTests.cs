using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Overview;

/// <summary>
/// F127.8: the overview is an admin route, behind the admin key, answering the day asked for (today by default); a day
/// that isn't one is a 400 with the reason.
/// </summary>
public class OverviewRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>With the key, the overview of the day: on a host with no log yet, no talks and no cost.</summary>
    [Test]
    public async Task WithTheKey_TheOverviewAnswers()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var response = await GetAsync(app, "/v1/admin/overview?day=2026-11-12", AdminKey);
        var reply = await response.Content.ReadFromJsonAsync<OverviewReply>(AdminJson.SnakeCase);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((reply!.Today.Talks, reply.Month.Talks, reply.Today.Cost), Is.EqualTo((0, 0, (decimal?)null)));
            Assert.That(reply.Currency, Is.EqualTo("USD"));
        });
    }

    /// <summary>Without the key it is refused, and a day that isn't one is a 400.</summary>
    [TestCase("/v1/admin/overview", "wrong", HttpStatusCode.Unauthorized)]
    [TestCase("/v1/admin/overview?day=yesterday", AdminKey, HttpStatusCode.BadRequest)]
    public async Task AWrongKeyOrDay_IsRefused(string path, string key, HttpStatusCode status)
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var response = await GetAsync(app, path, key);

        Assert.That(response.StatusCode, Is.EqualTo(status));
    }

    /// <summary>Asks the admin route with this key.</summary>
    private static Task<HttpResponseMessage> GetAsync(WebApplication app, string path, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", key);
        return app.GetTestClient().SendAsync(request);
    }
}
