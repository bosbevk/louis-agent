using LouisAgent.Web.Shared.Text;

namespace LouisAgent.Web.Shared.Tests.Text;

// FX22 review: the images a tool result gave are https only, as the shop rule is. An http image on Puls's https page is mixed content: the browser blocks or
// upgrades it, and the request would go out unencrypted. Written first: ImageLinks.Of took http images too.
public class ImageLinksTests
{
    [Test]
    public void AnHttpsImage_IsRead() =>
        Assert.That(ImageLinks.Of("![Bobotie](https://example.co.za/bobotie.jpg)"), Is.EqualTo(new[] { "https://example.co.za/bobotie.jpg" }));

    [Test]
    public void AnHttpImage_IsNotRead() =>
        Assert.That(ImageLinks.Of("![Bobotie](http://example.co.za/bobotie.jpg)"), Is.Empty);

    [Test]
    public void APlainLink_AndNoText_AreNoImages()
    {
        Assert.That(ImageLinks.Of("[x](https://example.co.za/x)"), Is.Empty);
        Assert.That(ImageLinks.Of(null), Is.Empty);
    }
}
