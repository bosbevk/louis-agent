using System.Net;
using System.Text;
using LouisAgent.Core.Tools.Web;

namespace LouisAgent.Core.Tests.Tools.Web;

// F30 step 7: a minion's price is used only when its quote is on the page. The check is code, no model: fetch the page
// through the SSRF guard, compare with whitespace collapsed and case folded, and say found, not found, empty or failed.
public class VerifyQuoteTests
{
    private const string Url = "https://www.checkers.co.za/p/beef-mince-500g";

    private const string ProductHtml = """
        <html><head><title>Beef Mince 500g | Checkers</title></head>
        <body>
          <nav><a href="/">Home</a></nav>
          <main>
            <h1>Checkers Beef Mince   500g</h1>
            <div class="price"><span>R</span><span>35.99</span></div>
            <p>Price&nbsp;valid until 20 Oct</p>
          </main>
          <footer><span class="unit">R71.98/kg</span></footer>
        </body></html>
        """;

    private static Task<IPAddress[]> PublicDns(string host, CancellationToken _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") });

    private static HttpResponseMessage Html(string html) =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") };

    private static WebTools Tools(Func<HttpRequestMessage, HttpResponseMessage> respond, IPageRenderer? renderer = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? dns = null) =>
        new(handler: new StubHandler(respond), resolve: dns ?? PublicDns, renderer: renderer);

    private sealed class FakeRenderer(string text) : IPageRenderer
    {
        public int Calls { get; private set; }
        public string Name => "fake";

        public Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new RenderedPage(url.ToString(), text));
        }
    }

    [Test]
    public async Task AQuoteOnThePage_IsFound()
    {
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, "Price valid until 20 Oct");

        Assert.That(result, Does.StartWith("Found:").And.Contain(Url));
    }

    [Test]
    public async Task AQuoteThatIsntThere_IsNotFound_AndSaysTheValueIsUnconfirmed()
    {
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, "R29.99 per 500g");

        Assert.That(result, Does.StartWith("Not found:").And.Contain("unconfirmed"));
    }

    [TestCase("checkers beef mince 500g")]
    [TestCase("  CHECKERS   Beef\nMince 500G ")]
    [TestCase("\"Price valid until 20 Oct\"")]
    [TestCase("R71.98/kg")]
    public async Task SpacingCaseQuotesAndThePagesOtherParts_DontMatter(string quote)
    {
        // A small model copies a quote with its own spacing and case; a price per kilo can sit outside <main>.
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, quote);

        Assert.That(result, Does.StartWith("Found:"), quote);
    }

    [Test]
    public async Task APageThatDoesntLoad_IsAnErrorWithItsStatus()
    {
        string result = await Tools(_ => new HttpResponseMessage(HttpStatusCode.NotFound)).VerifyQuote(Url, "R35.99");

        Assert.That(result, Does.StartWith("Error:").And.Contain("404"));
    }

    [Test]
    public async Task AJavaScriptPage_IsCheckedOnItsRenderedText()
    {
        // FetchUrl reads such a page as empty; the renderer sees what a browser shows.
        var renderer = new FakeRenderer("Beef Mince 500g\nR35.99\nAdd to cart");

        string result = await Tools(_ => Html("<html><body><div id=\"app\"></div><script>load()</script></body></html>"), renderer)
            .VerifyQuote(Url, "R35.99");

        Assert.That(result, Does.StartWith("Found:"));
        Assert.That(renderer.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task AnEmptyPageWithNoRenderer_SaysItCantBeChecked()
    {
        string result = await Tools(_ => Html("<html><body><div id=\"app\"></div></body></html>")).VerifyQuote(Url, "R35.99");

        Assert.That(result, Does.StartWith("Page empty:").And.Contain("unconfirmed"));
    }

    [Test]
    public async Task APageWithText_IsNeverRendered()
    {
        // ReadRenderedPage costs a credit: only an empty page is rendered.
        var renderer = new FakeRenderer("R35.99");

        await Tools(_ => Html(ProductHtml), renderer).VerifyQuote(Url, "R12.00");

        Assert.That(renderer.Calls, Is.Zero);
    }

    [Test]
    public async Task APrivateAddress_IsRefused_WithoutARequest()
    {
        int requests = 0;
        string result = await Tools(_ => { requests++; return Html(ProductHtml); }, dns: (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") }))
            .VerifyQuote("https://intranet.test/prices", "R35.99");

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(requests, Is.Zero);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\"\"")]
    public async Task AnEmptyQuote_IsRefused_WithoutARequest(string quote)
    {
        int requests = 0;
        string result = await Tools(_ => { requests++; return Html(ProductHtml); }).VerifyQuote(Url, quote);

        Assert.That(result, Does.StartWith("Error:"));
        Assert.That(requests, Is.Zero);
    }

    [Test]
    public async Task ANonHttpUrl_IsRefused() =>
        Assert.That(await Tools(_ => Html(ProductHtml)).VerifyQuote("file:///etc/passwd", "root"), Does.StartWith("Error:"));

    private static string ShellPage()
    {
        // What Pick n Pay sends (F30 hand check, 2026-10-17): a menu, a footer, and the product drawn later by scripts.
        string scripts = string.Concat(Enumerable.Repeat("<script>window.__state={\"product\":{\"id\":1}};render();</script>", 600));
        return "<html><head><title>Eskort Streaky Bacon 200g | PnP</title></head><body>" +
               "<nav><a href=\"/\">Home</a><a href=\"/food\">Food</a><a href=\"/cart\">Cart</a></nav>" +
               "<div id=\"app\">Loading…</div>" + scripts +
               "<footer>Copyright Pick n Pay. Terms. Privacy. Contact us.</footer></body></html>";
    }

    [Test]
    public async Task AShellPageWithAMenuAndAFooter_IsRendered_NotReportedAsNotFound()
    {
        // The hand check's false "Not found": the menu and footer left some text, so the page wasn't treated as empty.
        var renderer = new FakeRenderer("Eskort Streaky Bacon 200g\nESKORT R65.99\nAdd to trolley");

        string result = await Tools(_ => Html(ShellPage()), renderer).VerifyQuote(Url, "ESKORT R65.99");

        Assert.That(result, Does.StartWith("Found:"));
        Assert.That(renderer.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task AShellPageWithNoRenderer_IsPageEmpty()
    {
        string result = await Tools(_ => Html(ShellPage())).VerifyQuote(Url, "ESKORT R65.99");

        Assert.That(result, Does.StartWith("Page empty:"));
    }
}
