using System.Net;
using System.Text;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Robots;

namespace LouisAgent.Core.Tests.Tools.Web.Fetching;

/// <summary>
/// How a robots.txt is fetched for the policy: the text of a good answer, the bare status of a bad one, and a network
/// failure as no answer at all. Written first: <c>RobotsFileFetch.Through</c> threw NotImplementedException on the
/// red run.
/// </summary>
public class RobotsFileFetchTests
{
    private static readonly Uri RobotsUrl = new("https://a.com/robots.txt");

    /// <summary>The fetch over a fetcher on a stub web (no policy on it: robots.txt is always read).</summary>
    private static Func<Uri, CancellationToken, Task<RobotsFile>> Over(
        Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        RobotsFileFetch.Through(new PageFetcher(new StubHandler(respond), FakeWeb.PublicDns));

    /// <summary>A good answer gives its status and its text.</summary>
    [Test]
    public async Task AGoodAnswer_GivesTheStatusAndTheText()
    {
        var fetch = Over(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("User-agent: *\nDisallow: /x/\n", Encoding.UTF8, "text/plain"),
        });

        var file = await fetch(RobotsUrl, CancellationToken.None);

        Assert.That(file.Status, Is.EqualTo(200));
        Assert.That(file.Text, Is.EqualTo("User-agent: *\nDisallow: /x/\n"));
    }

    /// <summary>A 404 and a 503 give their status and no text.</summary>
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task ABadStatus_GivesTheStatusAndNoText(HttpStatusCode status)
    {
        var fetch = Over(_ => new HttpResponseMessage(status));

        var file = await fetch(RobotsUrl, CancellationToken.None);

        Assert.That(file.Status, Is.EqualTo((int)status));
        Assert.That(file.Text, Is.Null);
    }

    /// <summary>A network failure is not an exception to the policy, it is a file with no answer.</summary>
    [Test]
    public async Task ANetworkFailure_GivesNoStatusAndNoText()
    {
        var fetch = Over(_ => throw new HttpRequestException("connection refused"));

        var file = await fetch(RobotsUrl, CancellationToken.None);

        Assert.That(file.Status, Is.Null);
        Assert.That(file.Text, Is.Null);
    }
}
