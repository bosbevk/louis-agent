using LouisAgent.Choices;
using LouisAgent.Packs.Shops.Catalogues;

namespace LouisAgent.Packs.Shops.Tests;

// F18 step 7.5.2 follow-up (Louis, 2026-10-27, finding 2 B): a shop's display name is the pack's own table, never derived from the wire name, so the text the model reads stays as it is.
public class ShopKindsTests
{
    [Test]
    public void EveryShopHasADisplayName_ThatIsNotItsWireName()
    {
        foreach (var kind in Enum.GetValues<ShopKind>())
        {
            Assert.That(ShopKinds.DisplayName(kind), Is.Not.Empty, kind.ToString());
            Assert.That(ShopKinds.DisplayName(kind), Is.Not.EqualTo(kind.Wire()),
                "the display name is written out, apart from the wire name");
        }

        Assert.That(ShopKinds.DisplayName(ShopKind.Checkers), Is.EqualTo("Checkers"));
    }
}
