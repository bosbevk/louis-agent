using LouisAgent.Web.Components.Site.Pieces;

namespace LouisAgent.Web.Tests.Components.Site.Pieces;

/// <summary>
/// A panel's glass says what it writes on the panel (Louis: "transparent meaning see through glass and if it is
/// transparent then the amount and then type water, etc"): its colour's name, its type's, and how much colour.
/// </summary>
public class GlassLookTests
{
    /// <summary>The glass every panel has unless a site says otherwise: see-through, rippling like water.</summary>
    [Test]
    public void TheDefault_IsClearWater() =>
        Assert.That((GlassLook.Default.ColourName, GlassLook.Default.TypeName, GlassLook.Default.Style),
            Is.EqualTo(((string?)null, "water", (string?)null)));

    /// <summary>A tinted glass writes its colour, its type and its amount, kept between none and all of it.</summary>
    [TestCase(GlassColour.White, 25, GlassType.Frost, "white", "frost", "--web-glass-amount: 25%")]
    [TestCase(GlassColour.Grey, 140, GlassType.Plain, "grey", "plain", "--web-glass-amount: 100%")]
    [TestCase(GlassColour.Grey, -5, GlassType.Water, "grey", "water", "--web-glass-amount: 0%")]
    public void ATintedGlass_WritesItsColourTypeAndAmount(GlassColour colour, int amount, GlassType type,
        string name, string typeName, string style)
    {
        var glass = new GlassLook(colour, amount, type);

        Assert.That((glass.ColourName, glass.TypeName, glass.Style), Is.EqualTo((name, typeName, style)));
    }
}
