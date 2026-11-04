using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Readers.Listing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Evals.Studio;

/// <summary>
/// <c>GET /v1/admin/evals/checks</c>, what the studio's compose page reads: the runner's checks and readers, behind
/// the admin key, and a 503 with the runner's reason when it couldn't list them.
/// </summary>
public class StudioEndpointTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>A listing of one check and one reader, as the runner gave it.</summary>
    private static readonly CheckListingResult Listed = new(
        [
            new CheckTypeLine("within-time", CheckKind.FixedRule,
                JsonDocument.Parse("""{"type":["object","null"],"required":["seconds"]}""").RootElement.Clone())
        ],
        [new ReaderLine("rand-total", ReaderKind.Money)], null);

    /// <summary>Starts the API with a catalogue that answers so.</summary>
    private static async Task<WebApplication> StartAsync(CheckListingResult result)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, checks: new CheckList(() => result));
        await app.StartAsync();
        return app;
    }

    /// <summary>Asks the route, with the key unless the test leaves it off.</summary>
    private static Task<HttpResponseMessage> GetAsync(WebApplication app, bool withKey = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/admin/evals/checks");
        if (withKey) request.Headers.Add("x-api-key", AdminKey);
        return app.GetTestClient().SendAsync(request);
    }

    /// <summary>With the key, the checks and readers come back with their kinds' wire names and the schema.</summary>
    [Test]
    public async Task WithTheKey_TheChecksAndReadersComeBack()
    {
        await using var app = await StartAsync(Listed);

        var response = await GetAsync(app);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var check = reply.GetProperty("checks")[0];
        Assert.That((check.GetProperty("type").GetString(), check.GetProperty("kind").GetString(),
                check.GetProperty("parameters").GetProperty("required")[0].GetString()),
            Is.EqualTo(("within-time", "fixed-rule", "seconds")));
        var reader = reply.GetProperty("readers")[0];
        Assert.That((reader.GetProperty("name").GetString(), reader.GetProperty("kind").GetString()),
            Is.EqualTo(("rand-total", "money")));
    }

    /// <summary>Without the key, it's a 401.</summary>
    [Test]
    public async Task WithoutTheKey_ItIsA401()
    {
        await using var app = await StartAsync(Listed);

        Assert.That((await GetAsync(app, withKey: false)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>When the runner couldn't list them, it's a 503 with its reason.</summary>
    [Test]
    public async Task WhenTheRunnerCouldNotListThem_ItIsA503WithTheReason()
    {
        await using var app = await StartAsync(new CheckListingResult([], [], "the eval runner isn't at /nope"));

        var response = await GetAsync(app);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("isn't at /nope"));
    }

    /// <summary>
    /// A listing the runner gave is kept, and a failure is not, so a runner that comes up later is asked again.
    /// </summary>
    [Test]
    public void TheList_KeepsASuccess_ButAsksAgainAfterAFailure()
    {
        int asked = 0;
        var failing = new CheckList(() =>
        {
            asked++;
            return asked == 1 ? new CheckListingResult([], [], "not yet") : Listed;
        });

        var first = failing.Read();
        var second = failing.Read();
        var third = failing.Read();

        Assert.That((first.Failure, second.Failure, third.Failure, asked), Is.EqualTo(("not yet", (string?)null,
            (string?)null, 2)));
    }
}
