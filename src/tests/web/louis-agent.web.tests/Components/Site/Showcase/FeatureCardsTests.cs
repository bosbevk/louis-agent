using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Web.Tests.Components.Site.Showcase;

/// <summary>
/// The design's feature cards wear the theme's finish until a site switches a part off (Louis: "this should be
/// options to switch on or off on a panel"): the lens, the tilt and being picked.
/// </summary>
public class FeatureCardsTests
{
    /// <summary>The things a card shows, one is enough.</summary>
    private static readonly IReadOnlyList<Feature> One =
        [new Feature(FeatureFilm.Show, "It opens the right page.", "A guest asks.", "ShowPage")];

    /// <summary>With nothing switched off, each card bends, leans and can be picked by its title.</summary>
    [Test]
    public void ACard_BendsTiltsAndIsPicked_ByDefault()
    {
        using var ctx = new BunitContext();

        var card = ctx.Render<FeatureCards>(p => p.Add(c => c.Items, One)).Find("article");

        Assert.Multiple(() =>
        {
            Assert.That(card.HasAttribute("data-tilt"), Is.True);
            Assert.That(card.HasAttribute("data-lens"), Is.False);
            Assert.That(card.HasAttribute("data-piece"), Is.True);
            Assert.That(card.Id, Is.EqualTo("it-opens-the-right-page"));
        });
    }

    /// <summary>Switched off, a card is a still pane that bends nothing and can't be picked.</summary>
    [Test]
    public void ACard_SwitchedOff_IsStill()
    {
        using var ctx = new BunitContext();

        var card = ctx.Render<FeatureCards>(p => p.Add(c => c.Items, One)
            .Add(c => c.Lens, false).Add(c => c.Tilt, false).Add(c => c.Pick, false)).Find("article");

        Assert.Multiple(() =>
        {
            Assert.That(card.HasAttribute("data-tilt"), Is.False);
            Assert.That(card.GetAttribute("data-lens"), Is.EqualTo("off"));
            Assert.That(card.HasAttribute("data-piece"), Is.False);
        });
    }
}
