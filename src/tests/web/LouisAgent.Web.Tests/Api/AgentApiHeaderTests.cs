using System.Net;
using LouisAgent.Api.Contracts;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// Every call to the agent's API says it is the app's own (F61.2), so a signed-in person's cookie counts in place of
/// a key; with a key saved, both go. And a browser that holds the admin key says each session it opens is the admin's
/// (FX207), so the overview can tell the admin's own talks from visitors'; a visitor's browser holds no key and says
/// nothing.
/// </summary>
public class AgentApiHeaderTests
{
    /// <summary>Each call carries the app's mark, and the chat's key when one is saved.</summary>
    [TestCase(null)]
    [TestCase("k")]
    public async Task EveryCall_SaysItIsTheAppsOwn(string? apiKey)
    {
        string? requestedWith = null;
        string? key = null;
        var handler = new StubHandler(request =>
        {
            requestedWith = request.Headers.TryGetValues(ApiHeaders.RequestedWith, out var v) ? v.Single() : null;
            key = request.Headers.TryGetValues("x-api-key", out var k) ? k.Single() : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"rates":{}}""", System.Text.Encoding.UTF8, "application/json")
            };
        });
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings { ApiKey = apiKey });

        using var _ = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, "v1/rates"));

        Assert.Multiple(() =>
        {
            Assert.That(requestedWith, Is.EqualTo(ApiHeaders.RequestedWithWeb));
            Assert.That(key, Is.EqualTo(apiKey));
        });
    }

    /// <summary>The paths the stub was asked, and the key each carried.</summary>
    private static async Task<List<(string Path, string? Key)>> OpenAsync(string? adminKey,
        HttpStatusCode mineAnswers = HttpStatusCode.NoContent)
    {
        var asked = new List<(string Path, string? Key)>();
        var handler = new StubHandler(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            asked.Add((path, request.Headers.TryGetValues("x-api-key", out var key) ? key.Single() : null));
            return path.EndsWith("/mine", StringComparison.Ordinal)
                ? new HttpResponseMessage(mineAnswers)
                : new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("""{"session_id":"sess_1"}""", System.Text.Encoding.UTF8,
                        "application/json")
                };
        });
        var settings = new ChatSettings { ApiKey = "chat-key", AdminKey = adminKey };
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);

        await api.CreateSessionAsync([]);
        return asked;
    }

    /// <summary>With the admin key saved, the new session is marked the admin's, with that key.</summary>
    [Test]
    public async Task WithTheAdminKey_TheNewSessionIsMarkedTheAdmins()
    {
        var asked = await OpenAsync("admin-key");

        Assert.That(asked, Is.EqualTo(new[]
        {
            ("/v1/sessions", (string?)"chat-key"),
            ("/v1/admin/sessions/sess_1/mine", (string?)"admin-key")
        }));
    }

    /// <summary>Without it, only the session is opened.</summary>
    [Test]
    public async Task WithoutTheAdminKey_NothingIsMarked()
    {
        var asked = await OpenAsync(null);

        Assert.That(asked.Select(call => call.Path), Is.EqualTo(new[] { "/v1/sessions" }));
    }

    /// <summary>A key the API refuses never stops the chat: the session still opens.</summary>
    [Test]
    public void ARefusedAdminKey_StillOpensTheSession()
    {
        Assert.DoesNotThrowAsync(() => OpenAsync("old-key", HttpStatusCode.Unauthorized));
    }
}
