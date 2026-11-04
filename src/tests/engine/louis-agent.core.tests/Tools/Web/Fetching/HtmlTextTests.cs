using LouisAgent.Core.Tools.Web.Fetching;

namespace LouisAgent.Core.Tests.Tools.Web.Fetching;

// S (CL15): HTML as the readable text FetchUrl, the page renderer and the search providers show: the main content as
// markdown-like text, an inline fragment as one line, a whole body with every tag a space (VerifyQuote, FX16).
public class HtmlTextTests
{
    private const string ArticleHtml = """
        <html><head><title>Guide &amp; Notes</title><script>alert('x')</script></head>
        <body>
          <nav><a href="/home">Home</a></nav>
          <main>
            <h1>Install</h1>
            <p>Run the <code>dotnet</code> CLI. See <a href="/docs/setup">setup docs</a> or <a href="#top">top</a>.</p>
            <ul><li>First</li><li>Second</li></ul>
            <pre><code>dotnet new console
        dotnet run</code></pre>
            <style>.x{color:red}</style>
          </main>
          <footer>Copyright</footer>
        </body></html>
        """;

    [Test]
    public void TheMainContent_AsMarkdown()
    {
        var (title, text) = HtmlText.ToText(ArticleHtml, new Uri("https://example.com/guide/page"));

        Assert.That(title, Is.EqualTo("Guide & Notes"));
        Assert.That(text, Does.Contain("# Install"));
        Assert.That(text, Does.Contain("Run the `dotnet` CLI. See [setup docs](https://example.com/docs/setup) or top."));
        Assert.That(text, Does.Contain("- First\n- Second"));
        Assert.That(text, Does.Contain("```\ndotnet new console\ndotnet run\n```"));
        Assert.That(text, Does.Not.Contain("alert").And.Not.Contain("color:red").And.Not.Contain("Home").And.Not.Contain("Copyright"));
    }

    // A shop's product tile (the cook, F33): its photo is an image, often the only content of the link to the product,
    // and lazy-loaded, so the real file is in data-src or srcset while src holds a placeholder.
    private const string ProductTileHtml = """
        <html><body><main>
          <a href="/prod/Food/Onions/Brown-Onions-1-kg/_/A-20008215"><img src="data:image/gif;base64,R0lGOD" data-src="/images/onions.jpg" alt="Brown Onions 1 kg"></a>
          <a href="/prod/Food/Carrots/_/A-20008300">Carrots 1 kg <img srcset="//img.example.com/carrots-200.webp 200w, //img.example.com/carrots-400.webp 400w" alt="Carrots 1 kg"></a>
          <p><img src="/images/chakalaka.png" alt="KOO Chakalaka Mild 410 g"> R24,99</p>
          <img src="/icons/cart.svg" alt="Add to cart"><img src="/spacer.gif"><img src="/logo.png" alt="">
        </main></body></html>
        """;

    [Test]
    public void AnImage_WithAltText_BecomesAMarkdownImage()
    {
        var (_, text) = HtmlText.ToText(ProductTileHtml, new Uri("https://www.example.com/browse"));

        Assert.That(text, Does.Contain("![KOO Chakalaka Mild 410 g](https://www.example.com/images/chakalaka.png) R24,99"));
    }

    [Test]
    public void AnImageOnlyLink_KeepsItsHref_AndItsImage()
    {
        var (_, text) = HtmlText.ToText(ProductTileHtml, new Uri("https://www.example.com/browse"));

        Assert.That(text, Does.Contain("[![Brown Onions 1 kg](https://www.example.com/images/onions.jpg)](https://www.example.com/prod/Food/Onions/Brown-Onions-1-kg/_/A-20008215)"));
    }

    [Test]
    public void ALazyLoadedImage_UsesItsRealFile_FromDataSrcOrSrcset()
    {
        var (_, text) = HtmlText.ToText(ProductTileHtml, new Uri("https://www.example.com/browse"));

        Assert.That(text, Does.Contain("Carrots 1 kg ![Carrots 1 kg](https://img.example.com/carrots-200.webp)"));
        Assert.That(text, Does.Not.Contain("data:image"));
    }

    [Test]
    public void AnIcon_ASpacer_OrAnImageWithoutAlt_IsLeftOut()
    {
        var (_, text) = HtmlText.ToText(ProductTileHtml, new Uri("https://www.example.com/browse"));

        Assert.That(text, Does.Not.Contain("cart.svg").And.Not.Contain("spacer.gif").And.Not.Contain("logo.png"));
    }

    [Test]
    public void AnInlineFragment_LosesItsTags_AndSpacing_WithEntitiesDecoded() =>
        Assert.That(HtmlText.CleanInline(" <b>Beef</b>   &amp; \t <i>mince</i> "), Is.EqualTo("Beef & mince"));

    [Test]
    public void AWholeBody_WithEveryTagASpace_KeepsABadgeApartFromItsPrice()
    {
        // FX16: stripped with nothing between, a badge joined its price ("newr54.99"), which then wasn't seen as a price.
        string all = HtmlText.TagsAsSpaces("<style>.a{}</style><footer><span>new</span><b>R54.99</b> per kg</footer>");

        Assert.That(all, Does.Contain("new").And.Contain("R54.99").And.Not.Contain("newR54").And.Not.Contain(".a{}"));
    }
}
