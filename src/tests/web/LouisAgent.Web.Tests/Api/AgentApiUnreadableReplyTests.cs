using System.Net;
using System.Text;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// A reply that isn't JSON, such as a web page from a dev server or a proxy, is an API error a page can show, not a
/// crash that takes the whole app down with it. Every page already catches an API error.
/// </summary>
public class AgentApiUnreadableReplyTests
{
    /// <summary>The API client over a server that answers everything with a web page.</summary>
    private static AgentApi OverAWebPage() => new(new HttpClient(new StubHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<!DOCTYPE html><html></html>", Encoding.UTF8, "text/html")
        }))
    {
        BaseAddress = new Uri("http://localhost/")
    }, new ChatSettings());

    /// <summary>On the admin's wire, it says the reply couldn't be read.</summary>
    [Test]
    public void AWebPage_OnTheAdminsWire_IsAnApiError()
    {
        var error = Assert.ThrowsAsync<AgentApiException>(() =>
            OverAWebPage().GetAsync<ScreensReply>("v1/admin/screens", AdminJson.SnakeCase));

        Assert.That(error!.Message, Does.Contain("couldn't be read"));
    }

    /// <summary>On a site's own routes, the same.</summary>
    [Test]
    public void AWebPage_OnASitesRoute_IsAnApiError() =>
        Assert.ThrowsAsync<AgentApiException>(() => OverAWebPage().GetAsync<ScreensReply>("v1/anything"));
}
