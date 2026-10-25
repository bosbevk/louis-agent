using LouisAgent.Core.Pricing.Rates;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Search.Providers;

using static LouisAgent.Core.Tests.Tools.Web.FakeWeb;

namespace LouisAgent.Core.Tests.Tools.Web;

// FetchUrl: a page as readable text framed as untrusted, paged with startIndex; a PDF read like a document (F22); private addresses refused on every hop (the SSRF guard).
public class FetchUrlTests
{
    [Test]
    public async Task APdf_IsReadAsText()
    {
        // F22 step 3: a PDF on the web reads like a document, framed as untrusted content.
        byte[] pdf = LouisAgent.Core.Tests.Tools.Documents.DocumentFiles.Pdf(["Jaarverslag 2026"]);
        var tools = WebToolsOn(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(pdf) { Headers = { ContentType = new("application/pdf") } },
        });

        string result = await tools.FetchUrl("https://example.com/verslag.pdf");

        Assert.That(result, Does.Contain("--- Page 1 ---").And.Contain("Jaarverslag 2026"));
        Assert.That(result, Does.Contain("untrusted web content"));
    }

    [Test]
    public async Task APageOverTheLimit_WithNoStore_IsTooLarge()
    {
        // 58210fe: "too large" is decided by HttpRequestError.ConfigurationLimitExceeded. Its other tests go through the recorder or a stub's own
        // error; this is the path every live chat takes, where the limit is HttpClient.MaxResponseContentBufferSize.
        var tools = WebToolsOn(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[6 * 1024 * 1024]) });

        string result = await tools.FetchUrl("https://example.com/big");

        Assert.That(result, Does.Contain("larger than 5 MB"));
    }

    [Test]
    public async Task ADamagedPdf_IsAnAnswer_NotACrash()
    {
        // PR #28 review: only one of PdfPig's exception types was caught.
        var tools = WebToolsOn(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent("%PDF-1.7 not really"u8.ToArray()) { Headers = { ContentType = new("application/pdf") } },
        });

        Assert.That(await tools.FetchUrl("https://example.com/stukkend.pdf"), Does.StartWith("Couldn't read https://example.com/stukkend.pdf as a document"));
    }

    [Test]
    public async Task FetchUrl_ReturnsTextMarkedAsUntrusted()
    {
        var result = await WebToolsOn(_ => Html(ArticleHtml)).FetchUrl("https://example.com/guide");

        Assert.That(result, Does.StartWith("URL: https://example.com/guide"));
        Assert.That(result, Does.Contain("Title: Guide & Notes"));
        Assert.That(result, Does.Contain("untrusted web content"));
        Assert.That(result, Does.Contain("# Install"));
    }

    [Test]
    public async Task FetchUrl_PagesLongContentWithStartIndex()
    {
        string longText = string.Concat(Enumerable.Range(0, 300).Select(i => $"line {i:D3}\n"));
        var tools = WebToolsOn(_ => Html(longText, "text/plain"));

        var first = await tools.FetchUrl("https://example.com/log.txt", maxChars: 500);
        var second = await tools.FetchUrl("https://example.com/log.txt", maxChars: 500, startIndex: 500);

        Assert.That(first, Does.Contain("line 000").And.Contain("startIndex=500"));
        Assert.That(second, Does.Not.Contain("line 000").And.Contain("Showing characters 500-1000"));
    }

    [TestCase("ftp://example.com/file")]
    [TestCase("file:///etc/passwd")]
    [TestCase("not a url")]
    public async Task FetchUrl_RejectsNonHttpUrls(string url) =>
        Assert.That(await WebToolsOn(_ => Html("")).FetchUrl(url), Does.Contain("absolute http"));

    [TestCase("http://localhost:8080/admin")]
    [TestCase("http://127.0.0.1/")]
    [TestCase("http://169.254.169.254/latest/meta-data/")]
    [TestCase("http://192.168.1.10/")]
    [TestCase("http://[::1]/")]
    public async Task FetchUrl_BlocksLocalAndPrivateAddresses(string url)
    {
        bool requested = false;
        var result = await WebToolsOn(_ => { requested = true; return Html("secret"); }).FetchUrl(url);

        Assert.That(result, Does.Contain("refusing"));
        Assert.That(requested, Is.False, "no request may be sent to a blocked address");
    }

    [Test]
    public async Task FetchUrl_BlocksHostnamesResolvingToPrivateAddresses()
    {
        var tools = WebToolsOn(_ => Html("secret"), dns: (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") }));
        Assert.That(await tools.FetchUrl("https://intranet.example.com/"), Does.Contain("private or local address"));
    }

    [Test]
    public async Task FetchUrl_BlocksRedirectToPrivateAddress()
    {
        var requests = new List<Uri>();
        var tools = WebToolsOn(request =>
        {
            requests.Add(request.RequestUri!);
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("http://127.0.0.1/admin");
            return redirect;
        });

        var result = await tools.FetchUrl("https://example.com/go");

        Assert.That(result, Does.Contain("refusing"));
        Assert.That(requests, Has.Count.EqualTo(1), "the redirect target must never be requested");
    }

    [Test]
    public async Task FetchUrl_FollowsPublicRedirects()
    {
        var tools = WebToolsOn(request => request.RequestUri!.AbsolutePath == "/old"
            ? new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Headers = { Location = new Uri("/new", UriKind.Relative) } }
            : Html("<main><p>moved here</p></main>"));

        var result = await tools.FetchUrl("https://example.com/old");

        Assert.That(result, Does.Contain("URL: https://example.com/new").And.Contain("moved here"));
    }

    [Test]
    public async Task FetchUrl_RejectsBinaryContent()
    {
        var tools = WebToolsOn(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new("image/png") } } });
        Assert.That(await tools.FetchUrl("https://example.com/a.png"), Does.Contain("cannot be read as text"));
    }

    [Test]
    [Explicit("Hits the real internet")]
    public async Task Live_SearchAndFetch()
    {
        var tools = new WebTools(WebSearchProviderFactory.Create(AgentOptions.FromEnvironment()));

        var search = await tools.WebSearch("dotnet 10 what's new", maxResults: 3);
        var page = await tools.FetchUrl("https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview", maxChars: 1500);

        TestContext.Out.WriteLine(search);
        TestContext.Out.WriteLine(page);
        Assert.That(search, Does.Contain("1. "));
        Assert.That(page, Does.Contain(".NET 10"));
    }

    [Test]
    [Explicit("Hits the real network")]
    public async Task Live_ConnectTimeGuardBlocksPrivateTargets()
    {
        // Real handler: nip.io resolves 127.0.0.1.nip.io to 127.0.0.1.
        Assert.That(await new WebTools().FetchUrl("http://127.0.0.1.nip.io/"), Does.Contain("refusing"));
    }
}
