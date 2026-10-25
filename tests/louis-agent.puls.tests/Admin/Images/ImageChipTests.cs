using LouisAgent.Puls.Admin.Images;
using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Tests.Admin.Images;

// FX24 (Louis, 2026-10-26: "i need to see here recipe-image"): a conversation's row says whether its answer showed a photo of the dish; a run from before the count says nothing.
public class ImageChipTests
{
    [Test]
    public void AnAnswerWithAPhoto_ShowsAGreenImageChip() =>
        Assert.That(ImageChip.Of(1), Is.EqualTo(new PricedChip("image shown", "ok")));

    [Test]
    public void AnAnswerWithNone_ShowsAnOrangeNoImageChip() =>
        Assert.That(ImageChip.Of(0), Is.EqualTo(new PricedChip("no image", "warn")));

    [Test]
    public void AScoresDocumentFromBeforeTheCount_ShowsNoChip() =>
        Assert.That(ImageChip.Of(null), Is.Null);
}
