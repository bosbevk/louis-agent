using System.Net;
using LouisAgent.Api.Contracts;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// Every call to the agent's API says it is the app's own (F61.2), so a signed-in person's cookie counts in place of
/// a key; with a key saved, both go.
/// </summary>
public class AgentApiHeaderTests
{
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
}
