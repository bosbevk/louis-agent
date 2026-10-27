using LouisAgent.Core.Tools.Web.Fetching;

namespace LouisAgent.Core.Tests.Tools.Web.Fetching;

// FX24 (Louis, 2026-10-26, on cook-en's page af.hiloved.com/maklike-kerrie-hoender-resep/: "it has an image"): an AMP page draws its images as <amp-img>, not <img>, and has
// no og:image or JSON-LD, so the page text held none. An <amp-img> is an image like an <img>: kept with its alt text, left out without it (the related-article thumbnails).
// Written first: the text had no image from an AMP page.
public class HtmlTextAmpImageTests
{
    private const string AmpPage = """
                                   <html amp><head><title>Maklike Kerrie Hoender Resep</title></head><body>
                                   <article><h1>Maklike Kerrie Hoender Resep</h1>
                                   <amp-img width="1024" height="681" src="https://idata.hiloved.com/imgs/71c8af47bc3e39b6-1024x681.jpg" class="wp-post-image" layout="responsive" alt="Maklike Kerrie Hoender Resep"></amp-img>
                                   <p>500 g hoender</p>
                                   <amp-img src="https://idata.hiloved.com/imgs/80d8e84224cd33b5-120x86.jpg" width="120" height="86" layout="responsive" class="amp-related-image"></amp-img>
                                   </article></body></html>
                                   """;

    [Test]
    public void AnAmpImage_WithAltText_BecomesAMarkdownImage()
    {
        var (_, text) = HtmlText.ToText(AmpPage, new Uri("https://af.hiloved.com/maklike-kerrie-hoender-resep/"));

        Assert.That(text,
            Does.Contain(
                "![Maklike Kerrie Hoender Resep](https://idata.hiloved.com/imgs/71c8af47bc3e39b6-1024x681.jpg)"));
    }

    [Test]
    public void AnAmpImage_WithNoAlt_IsLeftOut_AsAnImgWithNoAltIs()
    {
        var (_, text) = HtmlText.ToText(AmpPage, new Uri("https://af.hiloved.com/maklike-kerrie-hoender-resep/"));

        Assert.That(text, Does.Not.Contain("80d8e84224cd33b5"));
    }
}
