using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared.Scoring;

/// <summary>The images an answer or a page shows, written as <c>![alt](url)</c>.</summary>
public class LinksImagesTests
{
    /// <summary>Every image link is read, a linked image too, and a plain link isn't.</summary>
    [Test]
    public void ItReadsEveryImageLink_NotAPlainLink() =>
        Assert.That(
            Links.ImageUrls(
                "![Bobotie](https://a.co.za/b.jpg) and [a link](https://a.co.za/x) and [![Dish](https://a.co.za/c.png)](https://a.co.za/y)"),
            Is.EqualTo(new[] { "https://a.co.za/b.jpg", "https://a.co.za/c.png" }));

    /// <summary>Text with no image gives no addresses.</summary>
    [Test]
    public void NoImage_IsNoUrls() => Assert.That(Links.ImageUrls("Resep."), Is.Empty);
}
