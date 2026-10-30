using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared.Scoring;

// F18 step 5.7: the images an answer or a page shows, as `![alt](url)`. Written first: `Links.ImageUrls` did not exist.
public class LinksImagesTests
{
    [Test]
    public void ItReadsEveryImageLink_NotAPlainLink() =>
        Assert.That(
            Links.ImageUrls(
                "![Bobotie](https://a.co.za/b.jpg) and [a link](https://a.co.za/x) and [![Dish](https://a.co.za/c.png)](https://a.co.za/y)"),
            Is.EqualTo(new[] { "https://a.co.za/b.jpg", "https://a.co.za/c.png" }));

    [Test]
    public void NoImage_IsNoUrls() => Assert.That(Links.ImageUrls("Resep."), Is.Empty);
}
