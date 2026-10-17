using System.Net;
using System.Text;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Rendering;

namespace LouisAgent.Core.Tests.Tools.Web;

// F30 step 7: a minion's price is used only when its quote is on the page. The check is code, no model: fetch the page
// through the SSRF guard, compare with whitespace collapsed and case folded, and say found, not found, empty or failed.
public class VerifyQuoteTests
{
    private const string Url = "https://www.checkers.co.za/p/beef-mince-500g";
    private const string Beef = "Checkers Beef Mince 500g";

    private const string ProductHtml = """
        <html><head><title>Beef Mince 500g | Checkers</title></head>
        <body>
          <nav><a href="/">Home</a></nav>
          <main>
            <h1>Checkers Beef Mince   500g</h1>
            <div class="price"><span>R</span><span>35.99</span></div>
            <p>Price&nbsp;valid until 20 Oct</p>
          </main>
          <footer><span class="unit">Unit price R71.98/kg</span></footer>
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
        public CancellationToken Token { get; private set; }
        public string Name => "fake";

        public Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default)
        {
            Calls++;
            Token = cancellationToken;
            return Task.FromResult(new RenderedPage(url.ToString(), text));
        }
    }

    // Review of PR #56: a cancelled turn kept its browser page; the turn's token reaches the renderer from both tools
    // that render (Microsoft.Extensions.AI fills a CancellationToken parameter and leaves it out of the tool's schema).

    [Test]
    public async Task ReadRenderedPage_HandsTheTurnsCancellationToTheRenderer()
    {
        var renderer = new FakeRenderer("R22.99");
        using var turn = new CancellationTokenSource();

        await Tools(_ => Html(""), renderer).ReadRenderedPage("https://www.pnp.co.za/p/1", cancellationToken: turn.Token);

        Assert.That(renderer.Token, Is.EqualTo(turn.Token));
    }

    [Test]
    public async Task VerifyQuote_HandsTheTurnsCancellationToTheRenderer()
    {
        var renderer = new FakeRenderer("Beef mince R59.99");
        using var turn = new CancellationTokenSource();

        await Tools(_ => Html("<html><body><div id=\"app\"></div></body></html>"), renderer)
            .VerifyQuote("https://www.pnp.co.za/p/1", "R59.99", "Beef mince", cancellationToken: turn.Token);

        Assert.That(renderer.Calls, Is.EqualTo(1), "an empty page is rendered");
        Assert.That(renderer.Token, Is.EqualTo(turn.Token));
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
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, "Beef Mince R29.99");

        Assert.That(result, Does.StartWith("Not found:").And.Contain("unconfirmed"));
    }

    [TestCase("checkers beef mince 500g")]
    [TestCase("  CHECKERS   Beef\nMince 500G ")]
    [TestCase("\"Price valid until 20 Oct\"")]
    [TestCase("unit price R71.98/kg")]
    public async Task SpacingCaseQuotesAndThePagesOtherParts_DontMatter(string quote)
    {
        // A small model copies a quote with its own spacing and case; a price per kilo can sit outside <main>.
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, quote);

        Assert.That(result, Does.StartWith("Found:"), quote);
    }

    [Test]
    public async Task APageThatDoesntLoad_IsAnErrorWithItsStatus()
    {
        string result = await Tools(_ => new HttpResponseMessage(HttpStatusCode.NotFound)).VerifyQuote(Url, "R35.99", Beef);

        Assert.That(result, Does.StartWith("Error:").And.Contain("404"));
    }

    [Test]
    public async Task AJavaScriptPage_IsCheckedOnItsRenderedText()
    {
        // FetchUrl reads such a page as empty; the renderer sees what a browser shows.
        var renderer = new FakeRenderer("Beef Mince 500g\nR35.99\nAdd to cart");

        string result = await Tools(_ => Html("<html><body><div id=\"app\"></div><script>load()</script></body></html>"), renderer)
            .VerifyQuote(Url, "R35.99", "Beef Mince 500g");

        Assert.That(result, Does.StartWith("Found:"));
        Assert.That(renderer.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task AnEmptyPageWithNoRenderer_SaysItCantBeChecked()
    {
        string result = await Tools(_ => Html("<html><body><div id=\"app\"></div></body></html>")).VerifyQuote(Url, "R35.99", Beef);

        Assert.That(result, Does.StartWith("Page empty:").And.Contain("unconfirmed"));
    }

    [Test]
    public async Task APageWithText_IsNeverRendered()
    {
        // ReadRenderedPage is slow (and, with Tavily, costs a credit): only an empty page is rendered.
        var renderer = new FakeRenderer("R35.99");

        await Tools(_ => Html(ProductHtml), renderer).VerifyQuote(Url, "R12.00", Beef);

        Assert.That(renderer.Calls, Is.Zero);
    }

    [Test]
    public async Task APrivateAddress_IsRefused_WithoutARequest()
    {
        int requests = 0;
        string result = await Tools(_ => { requests++; return Html(ProductHtml); }, dns: (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") }))
            .VerifyQuote("https://intranet.test/prices", "R35.99", Beef);

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

    // FX16 (the PR #51 review): a quote that is only a price matched any product at that price on the page, so
    // "Found" said more than was checked. A price is checked against its product: right after the product's name.
    private const string ListUrl = "https://www.pnp.co.za/c/eggs-dairy";

    private const string ListHtml = """
        <html><body><main><ul>
          <li><h3>PnP Large Eggs 6 Pack</h3><span class="price">R22.99</span><a>Add to trolley</a></li>
          <li><h3>PnP Salted Butter 500g</h3><span class="price">R54.99</span><a>Add to trolley</a></li>
          <li><h3>PnP Full Cream Milk 2L</h3><span class="price">R22.99</span><a>Add to trolley</a></li>
        </ul></main></body></html>
        """;

    [TestCase("R22.99")]
    [TestCase("\"R 22,99\"")]
    [TestCase("R119.98/kg")]
    [TestCase("R22.99 each")]
    [TestCase("Now R22.99")]
    [TestCase("only R22.99 per kg")]
    [TestCase("Was R29.99 Save R7.00")]
    public async Task APriceOnlyQuote_WithoutItsProduct_IsRefused_WithoutARequest(string quote)
    {
        int requests = 0;
        string result = await Tools(_ => { requests++; return Html(ListHtml); }).VerifyQuote(ListUrl, quote);

        Assert.That(result, Does.StartWith("Error:").And.Contain("product"));
        Assert.That(requests, Is.Zero);
    }

    [TestCase("PnP Large Eggs 6 Pack")]
    [TestCase("pnp full cream milk 2l")]
    public async Task APriceRightAfterItsProduct_IsFound(string product)
    {
        string result = await Tools(_ => Html(ListHtml)).VerifyQuote(ListUrl, "R22.99", product);

        Assert.That(result, Does.StartWith("Found:"), product);
    }

    [Test]
    public async Task AnotherProductsPrice_IsNotFoundWithTheProduct()
    {
        // The bug: R22.99 is on the page (the eggs, the milk), but the butter is R54.99.
        string result = await Tools(_ => Html(ListHtml)).VerifyQuote(ListUrl, "R22.99", "PnP Salted Butter 500g");

        Assert.That(result, Does.StartWith("Not found with the product:").And.Contain("PnP Salted Butter 500g").And.Contain("unconfirmed"));
    }

    [Test]
    public async Task AProductThePageDoesntShow_IsNotFoundWithTheProduct()
    {
        string result = await Tools(_ => Html(ListHtml)).VerifyQuote(ListUrl, "R22.99", "Free Range Eggs 18 Pack");

        Assert.That(result, Does.StartWith("Not found with the product:"));
    }

    [Test]
    public async Task APriceSplitAcrossTags_StillMatches()
    {
        // ProductHtml draws the price as <span>R</span><span>35.99</span>: "r 35.99" once its tags are spaces, read as r35.99.
        string result = await Tools(_ => Html(ProductHtml)).VerifyQuote(Url, "R35.99", Beef);

        Assert.That(result, Does.StartWith("Found:"));
    }

    // The PR #54 review: a special shows the old price between the name and the price ("Was R29.99 · R22.99").
    private const string SpecialsHtml = """
        <html><body><main><ul>
          <li><h3>PnP Large Eggs 6 Pack</h3><span>Was R29.99</span><span class="price">R22.99</span></li>
          <li><h3>PnP Salted Butter 500g</h3><span>Save R7.00</span><span class="price">R54.99</span></li>
          <li><h3>PnP Full Cream Milk 2L</h3><span class="price">R22.99</span></li>
        </ul></main></body></html>
        """;

    [TestCase("PnP Large Eggs 6 Pack", "R22.99")]
    [TestCase("PnP Salted Butter 500g", "R54.99")]
    public async Task ASpecialsOldPriceOrSaving_DoesntHideTheProductsPrice(string product, string quote)
    {
        string result = await Tools(_ => Html(SpecialsHtml)).VerifyQuote(ListUrl, quote, product);

        Assert.That(result, Does.StartWith("Found:"), product);
    }

    [TestCase("PnP Large Eggs 6 Pack", "R29.99")]
    [TestCase("PnP Salted Butter 500g", "R7.00")]
    public async Task TheOldPriceOrTheSaving_IsNotTheProductsPrice(string product, string quote)
    {
        string result = await Tools(_ => Html(SpecialsHtml)).VerifyQuote(ListUrl, quote, product);

        Assert.That(result, Does.StartWith("Not found with the product:"), quote);
    }

    [Test]
    public async Task ASpecialStillDoesntLetTheNextProductsPriceThrough()
    {
        // The butter's saving is skipped, its own price is not: the milk's R22.99 is not the butter's.
        string result = await Tools(_ => Html(SpecialsHtml)).VerifyQuote(ListUrl, "R22.99", "PnP Salted Butter 500g");

        Assert.That(result, Does.StartWith("Not found with the product:"));
    }

    // The PR #54 re-review: tags stripped with nothing between them joined a badge to the price ("500gnewr54.99"), so
    // the butter's own price lost its word boundary, wasn't seen as a price, and the milk's R22.99 passed for it.
    private const string BadgeHtml = """
        <html><body><main><ul>
          <li><h3>PnP Large Eggs 6 Pack</h3><span class="price">R22.99</span></li>
          <li><h3>PnP Salted Butter 500g</h3><span>New</span><span class="price">R54.99</span></li>
          <li><h3>PnP Full Cream Milk 2L</h3><span class="price">R22.99</span></li>
        </ul></main></body></html>
        """;

    [Test]
    public async Task ABadgeBeforeThePrice_DoesntLetTheNextProductsPriceThrough()
    {
        string result = await Tools(_ => Html(BadgeHtml)).VerifyQuote(ListUrl, "R22.99", "PnP Salted Butter 500g");

        Assert.That(result, Does.StartWith("Not found with the product:"));
    }

    [Test]
    public async Task ABadgeBeforeThePrice_StillFindsTheProductsOwnPrice()
    {
        string result = await Tools(_ => Html(BadgeHtml)).VerifyQuote(ListUrl, "R54.99", "PnP Salted Butter 500g");

        Assert.That(result, Does.StartWith("Found:"));
    }
}
