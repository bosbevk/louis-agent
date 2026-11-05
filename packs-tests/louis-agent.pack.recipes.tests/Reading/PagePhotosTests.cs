using LouisAgent.Packs.Recipes.Reading;

namespace LouisAgent.Packs.Recipes.Tests.Reading;

/// <summary>
/// Where a page's photo might be, best first. Each case here is a real page: Zola Nene declares none and its photo has
/// no description; The Food Fox declares a dead server, with a working copy in its <c>srcset</c>.
/// </summary>
public class PagePhotosTests
{
    private static readonly Uri Page = new("https://example.com/recipes/pie/");

    /// <summary>The addresses a page gives, as text.</summary>
    private static string[] Candidates(string html, Uri? declared = null) =>
        [.. PagePhotos.Candidates(html, Page, declared).Select(uri => uri.AbsoluteUri)];

    /// <summary>A photo the page declares for machines comes first.</summary>
    [Test]
    public void TheDeclaredPhoto_ComesFirst()
    {
        const string html = """
                            <head><meta property="og:image" content="https://example.com/dish.jpg"></head>
                            <body><h1>Pie</h1><img src="https://example.com/body.jpg"></body>
                            """;

        Assert.That(Candidates(html), Is.EqualTo(new[] { "https://example.com/dish.jpg", "https://example.com/body.jpg" }));
    }

    /// <summary>What the recipe's own data names stands in for what the page's tags declare.</summary>
    [Test]
    public void ARecipesOwnPhoto_ComesFirst() =>
        Assert.That(Candidates("<h1>Pie</h1><img src=\"https://example.com/body.jpg\">", new Uri("https://example.com/data.jpg")),
            Is.EqualTo(new[] { "https://example.com/data.jpg", "https://example.com/body.jpg" }));

    /// <summary>A page that declares nothing still has the first real image after its title, lazy or not.</summary>
    [TestCase("""<h1>Pie</h1><img src="data:image/gif;base64,R0" alt="" data-src="https://example.com/pie.jpeg">""")]
    [TestCase("""<h1>Pie</h1><img src="https://example.com/pie.jpeg" alt="">""")]
    [TestCase("""<img src="https://example.com/before.jpg"><h1>Pie</h1><img src="https://example.com/pie.jpeg">""")]
    public void ThePhotoAfterTheTitle_IsFound(string html) =>
        Assert.That(Candidates(html), Is.EqualTo(new[] { "https://example.com/pie.jpeg" }));

    /// <summary>A <c>srcset</c> holds the site's real copy where <c>src</c> may point at an old server.</summary>
    [Test]
    public void ASrcset_ComesBeforeASrc()
    {
        const string html = """
                            <h1>Bobotie</h1><img src="http://old.example.org/b.jpg"
                            srcset="https://example.com/b.jpg 996w, https://example.com/b-300.jpg 300w">
                            """;

        Assert.That(Candidates(html), Is.EqualTo(new[] { "https://example.com/b.jpg" }));
    }

    /// <summary>A logo, an avatar, a share button or a site header is no photo of the dish.</summary>
    [TestCase("https://example.com/uploads/logo.png")]
    [TestCase("https://secure.gravatar.com/avatar/9edf?s=85")]
    [TestCase("https://example.com/wp-content/plugins/share/print.png")]
    [TestCase("https://example.com/uploads/2020/02/TFF-header.png")]
    [TestCase("https://example.com/themes/site/restaurant.jpg")]
    [TestCase("https://example.com/icon.svg")]
    public void PageFurniture_IsNoPhoto(string file) =>
        Assert.That(Candidates($"<h1>Pie</h1><img src=\"{file}\">"), Is.Empty);

    /// <summary>
    /// An image server's copy can be gone while the site's own is fine, so the site's comes first: Zola Nene's
    /// chakalaka is a 404 at i0.wp.com and loads from zolanene.com.
    /// </summary>
    [Test]
    public void AnImageServersCopy_ComesAfterTheSitesOwn()
    {
        const string html = """<h1>Pie</h1><img src="https://i0.wp.com/www.zola.example/up/pie.jpg?w=1080">""";

        Assert.That(Candidates(html), Is.EqualTo(new[]
        {
            "https://www.zola.example/up/pie.jpg", "https://i0.wp.com/www.zola.example/up/pie.jpg?w=1080"
        }));
    }

    /// <summary>A page's text keeps no image: most are related recipes, and the print names the checked one.</summary>
    [TestCase("Bobotie\n\n![Avatar](https://example.com/a.png)\n\nMince", "Bobotie\n\nMince")]
    [TestCase("[![logo](https://example.com/l.jpg)](https://example.com/) ![x](https://example.com/2.jpg)\n\nMince", "Mince")]
    [TestCase("Plain words, with [a link](https://example.com/) kept.", "Plain words, with [a link](https://example.com/) kept.")]
    public void ThePagesText_KeepsNoImage(string text, string expected) =>
        Assert.That(PagePhotos.WithoutImages(text), Is.EqualTo(expected));
}
