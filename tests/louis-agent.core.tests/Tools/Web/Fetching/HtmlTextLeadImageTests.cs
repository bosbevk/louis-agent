using LouisAgent.Core.Tools.Web.Fetching;

namespace LouisAgent.Core.Tests.Tools.Web.Fetching;

// FX24 (Louis, 2026-10-26: "i checked the links they all have images we just did not pull it"): a recipe page's photo is often in its JSON-LD Recipe.image or its og:image,
// not in an <img> with alt text inside <main>, which is all the page text kept. The page's lead image is read from those and put first, as ![title](url), so the cook can show it.
// Written first: the text held none of them.
public class HtmlTextLeadImageTests
{
    private static readonly Uri Page = new("https://recipes.example.co.za/pap-en-wors-tert/");

    private static string Html(string head, string body = "<main><h1>Pap en wors tert</h1><p>500 g wors</p></main>") =>
        $"<html><head><title>Pap en wors tert</title>{head}</head><body>{body}</body></html>";

    private static string JsonLd(string json) => $"<script type=\"application/ld+json\">{json}</script>";

    [Test]
    public void ARecipesJsonLdImage_IsTheFirstLine()
    {
        var (_, text) = HtmlText.ToText(Html(JsonLd("""{"@context":"https://schema.org","@type":"Recipe","name":"Pap","image":"https://recipes.example.co.za/wp-content/uploads/pap.jpg"}""")), Page);

        Assert.That(text.Split('\n')[0], Is.EqualTo("![Pap en wors tert](https://recipes.example.co.za/wp-content/uploads/pap.jpg)"));
    }

    [Test]
    public void TheImage_MayBeAnArray_AnImageObject_OrInAGraph()
    {
        string graph = JsonLd("""{"@graph":[{"@type":"WebSite"},{"@type":["Recipe"],"image":[{"@type":"ImageObject","url":"https://r.example.co.za/a.jpg"},"https://r.example.co.za/b.jpg"]}]}""");
        string single = JsonLd("""{"@type":"Recipe","image":{"@type":"ImageObject","url":"https://r.example.co.za/c.jpg"}}""");
        string list = JsonLd("""[{"@type":"Recipe","image":["https://r.example.co.za/d.jpg"]}]""");

        Assert.That(HtmlText.ToText(Html(graph), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/a.jpg)"));
        Assert.That(HtmlText.ToText(Html(single), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/c.jpg)"));
        Assert.That(HtmlText.ToText(Html(list), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/d.jpg)"));
    }

    [Test]
    public void ARelativeImage_IsMadeAbsoluteFromThePage()
    {
        var (_, text) = HtmlText.ToText(Html(JsonLd("""{"@type":"Recipe","image":"/wp-content/pap.jpg"}""")), Page);

        Assert.That(text, Does.StartWith("![Pap en wors tert](https://recipes.example.co.za/wp-content/pap.jpg)"));
    }

    [Test]
    public void WithNoJsonLdImage_TheOgImageIsUsed_AndTheRecipesOwnWinsOverIt()
    {
        string og = """<meta property="og:image" content="https://r.example.co.za/og.jpg">""";

        Assert.That(HtmlText.ToText(Html(og), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/og.jpg)"));
        Assert.That(HtmlText.ToText(Html(og + JsonLd("""{"@type":"Recipe","image":"https://r.example.co.za/recipe.jpg"}""")), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/recipe.jpg)"));
    }

    [Test]
    public void AnOgImage_WithTheContentAttributeFirst_IsStillRead() =>
        Assert.That(HtmlText.ToText(Html("""<meta content="https://r.example.co.za/og.jpg" property="og:image" />"""), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/og.jpg)"));

    [Test]
    public void AnImageThePageAlreadyShows_IsNotAddedTwice()
    {
        string body = """<main><h1>Pap</h1><img src="https://r.example.co.za/pap.jpg" alt="Pap en wors tert"><p>500 g wors</p></main>""";

        var (_, text) = HtmlText.ToText(Html(JsonLd("""{"@type":"Recipe","image":"https://r.example.co.za/pap.jpg"}"""), body), Page);

        Assert.That(text.Split("https://r.example.co.za/pap.jpg").Length - 1, Is.EqualTo(1));
    }

    [TestCase("""{"@type":"Recipe","image":"data:image/png;base64,AAAA"}""")]
    [TestCase("""{"@type":"Recipe","image":"https://r.example.co.za/logo.svg"}""")]
    [TestCase("""{"@type":"Organization","image":"https://r.example.co.za/the-company.jpg"}""")]
    [TestCase("""{"@type":"Recipe","image":""")]
    public void NoImage_OrOneThatCannotBeShown_AddsNothing(string json)
    {
        var (_, text) = HtmlText.ToText(Html(JsonLd(json)), Page);

        Assert.That(text, Does.Not.Contain("!["));
    }

    // Review of FX24: core names no product or domain (ADR 0027), so the lead image is the page's main JSON-LD entity's, whatever its type: a recipe, an article, a product. The entities
    // that describe the site, not the page's subject (an organization, a website, a web page, a person, an image), are skipped.
    [Test]
    public void ANonRecipePage_GivesItsMainEntitiesImage_SkippingTheEntitiesThatDescribeTheSite()
    {
        string graph = JsonLd("""{"@graph":[{"@type":"Organization","image":"https://r.example.co.za/logo-photo.jpg"},{"@type":"WebSite"},{"@type":"WebPage","image":"https://r.example.co.za/page.jpg"},{"@type":"Person","image":"https://r.example.co.za/author.jpg"},{"@type":"NewsArticle","image":["https://r.example.co.za/story.jpg"]}]}""");

        Assert.That(HtmlText.ToText(Html(graph), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/story.jpg)"));
    }

    [Test]
    public void APageWhoseOnlyEntitiesDescribeTheSite_FallsBackToItsOgImage()
    {
        string only = JsonLd("""{"@graph":[{"@type":"Organization","image":"https://r.example.co.za/the-company.jpg"},{"@type":"ImageObject","url":"https://r.example.co.za/x.jpg"}]}""");
        string og = """<meta property="og:image" content="https://r.example.co.za/og.jpg">""";

        Assert.That(HtmlText.ToText(Html(only + og), Page).Text, Does.StartWith("![Pap en wors tert](https://r.example.co.za/og.jpg)"));
    }

    // The page's main item, whatever it is (ADR 0027: core names no domain). A breadcrumb trail is the page's furniture too.
    [Test]
    public void AnArticlePage_GivesItsNewsArticlesImage() =>
        Assert.That(HtmlText.ToText(Html(JsonLd("""{"@type":"NewsArticle","headline":"Big news","image":["https://r.example.co.za/story.jpg"]}""")), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/story.jpg)"));

    [Test]
    public void AnOrganizationsLogo_BesideARecipe_IsNotTheLeadImage() =>
        Assert.That(HtmlText.ToText(Html(JsonLd("""[{"@type":"Organization","image":"https://r.example.co.za/logo-photo.jpg"},{"@type":"Recipe","image":"https://r.example.co.za/dish.jpg"}]""")), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/dish.jpg)"));

    [Test]
    public void AnOrganizationAndAWebSiteOnly_GiveNoImageFromJsonLd_SoOgImageIsUsed()
    {
        string site = JsonLd("""{"@graph":[{"@type":"Organization","image":"https://r.example.co.za/the-company.jpg"},{"@type":"WebSite","image":"https://r.example.co.za/site.jpg"}]}""");

        Assert.That(HtmlText.ToText(Html(site), Page).Text, Does.Not.Contain("!["), "nothing declared by the page itself");
        Assert.That(HtmlText.ToText(Html(site + """<meta property="og:image" content="https://r.example.co.za/og.jpg">"""), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/og.jpg)"));
    }

    [Test]
    public void ABreadcrumbList_IsNoSubject() =>
        Assert.That(HtmlText.ToText(Html(JsonLd("""{"@graph":[{"@type":"BreadcrumbList","image":"https://r.example.co.za/crumb.jpg"},{"@type":"Article","image":"https://r.example.co.za/story.jpg"}]}""")), Page).Text,
            Does.StartWith("![Pap en wors tert](https://r.example.co.za/story.jpg)"));

    [Test]
    public void ALazyImageWithItsFileInAnotherAttribute_IsKept()
    {
        string body = """<main><img src="data:image/gif;base64,R0lGOD" data-lazy-src="https://r.example.co.za/lazy.jpg" alt="Pap"><img src="p.gif" data-orig-file="https://r.example.co.za/orig.jpg" alt="Wors"></main>""";

        var (_, text) = HtmlText.ToText(Html("", body), Page);

        Assert.That(text, Does.Contain("![Pap](https://r.example.co.za/lazy.jpg)").And.Contain("![Wors](https://r.example.co.za/orig.jpg)"));
    }
}
