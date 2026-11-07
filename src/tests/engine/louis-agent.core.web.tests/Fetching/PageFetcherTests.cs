using LouisAgent.Composition.Web.Messages;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Messages;
using System.Net;
using System.Text;
using LouisAgent.Core.Guardrails.Network;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Web.Fetching;
using LouisAgent.Core.Web.Messages;
using LouisAgent.Core.Web.Robots;

namespace LouisAgent.Core.Web.Tests.Fetching;

/// <summary>
/// The fetcher with a robots policy: a disallowed address is refused before anything is sent, on the first request
/// and on every redirect hop, and a page that says no to AI readers is refused once it answers. Written first: the
/// fetcher took the policy and ignored it on the red run. Without a policy nothing changes.
/// </summary>
public class PageFetcherTests
{
    private const string Closes = "User-agent: *\nDisallow: /private/\n";

    /// <summary>A policy over a fake robots.txt for every site.</summary>
    private static RobotsPolicy PolicyFor(string robots) =>
        new((_, _) => Task.FromResult(new RobotsFile(200, robots)), TimeProvider.System);

    /// <summary>A fetcher on a stub web with a public address for every host, and the policy you give.</summary>
    private static PageFetcher FetcherOn(StubHandler web, RobotsPolicy? robots) =>
        new(web, FakeDns.Public, robots: robots);

    /// <summary>A plain 200 page, with an optional header.</summary>
    private static HttpResponseMessage Page(string html, string? header = null, string? value = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html"),
        };
        if (header is not null) response.Headers.Add(header, value);
        return response;
    }

    /// <summary>A disallowed address is refused with the reason, and nothing is sent to the site.</summary>
    [Test]
    public void ADisallowedAddress_IsRefused_BeforeTheRequestIsSent()
    {
        var web = new StubHandler(_ => Page("<p>secret</p>"));
        var fetcher = FetcherOn(web, PolicyFor(Closes));

        var failure = Assert.ThrowsAsync<BlockedAddressException>(() =>
            fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/private/x")));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.RobotsDisallows,
            new
            {
                Host = "a.com",
                Path = "/private/x"
            })));
        Assert.That(web.Requests, Is.Empty);
    }

    /// <summary>A redirect to a disallowed path is refused at that hop, after the first page was asked for.</summary>
    [Test]
    public void ARedirectToADisallowedPath_IsRefusedAtThatHop()
    {
        var web = new StubHandler(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("/private/moved", UriKind.Relative);
            return redirect;
        });
        var fetcher = FetcherOn(web, PolicyFor(Closes));

        var failure = Assert.ThrowsAsync<BlockedAddressException>(() =>
            fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/recipes/old")));

        Assert.That(failure.Message, Does.Contain("/private/moved"));
        Assert.That(web.Requests, Has.Count.EqualTo(1), "only the first address was requested");
    }

    /// <summary>A page that answers with X-Robots-Tag noai is refused with the signal named.</summary>
    [Test]
    public void AnAnswerWithXRobotsTagNoAi_IsRefused()
    {
        var web = new StubHandler(_ => Page("<p>hello</p>", "X-Robots-Tag", "noindex, noai"));
        var fetcher = FetcherOn(web, PolicyFor(Closes));

        var failure = Assert.ThrowsAsync<BlockedAddressException>(() =>
            fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/recipes/")));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.PageRefusesAiReaders,
            new
            {
                Host = "a.com",
                Signal = "X-Robots-Tag noai"
            })));
    }

    /// <summary>A page whose body carries the noai meta is refused too.</summary>
    [Test]
    public void AnAnswerWithTheNoAiMeta_IsRefused()
    {
        var web = new StubHandler(_ => Page("<head><meta name=\"robots\" content=\"noai\"></head><p>hi</p>"));
        var fetcher = FetcherOn(web, PolicyFor(Closes));

        var failure = Assert.ThrowsAsync<BlockedAddressException>(() =>
            fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/recipes/")));

        Assert.That(failure.Message, Is.EqualTo(MessageText.Format(WebToolMessage.PageRefusesAiReaders,
            new
            {
                Host = "a.com",
                Signal = "meta robots noai"
            })));
    }

    /// <summary>An allowed page is returned, and its body can still be read by whoever asked.</summary>
    [Test]
    public async Task AnAllowedPage_IsReturned_AndItsBodyStillReads()
    {
        var web = new StubHandler(_ => Page("<p>bobotie</p>"));
        var fetcher = FetcherOn(web, PolicyFor(Closes));

        var (response, finalUri) = await fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/recipes/"));

        Assert.That(finalUri, Is.EqualTo(new Uri("https://a.com/recipes/")));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("<p>bobotie</p>"));
    }

    /// <summary>
    /// With no policy the fetcher behaves as it always did: a closed path and a noai page are both read.
    /// </summary>
    [Test]
    public async Task WithoutAPolicy_NothingChanges()
    {
        var web = new StubHandler(_ => Page("<meta name=\"robots\" content=\"noai\">", "X-Robots-Tag", "noai"));
        var fetcher = FetcherOn(web, null);

        var (response, _) = await fetcher.GetFollowingRedirectsAsync(new Uri("https://a.com/private/x"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(web.Requests, Has.Count.EqualTo(1));
    }
}
