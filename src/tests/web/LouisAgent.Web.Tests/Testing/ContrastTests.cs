using LouisAgent.Web.Testing.Styles;

namespace LouisAgent.Web.Tests.Testing;

/// <summary>
/// F169.1: the contrast sum the theme tests ask is the web's own. W3C's technique pages give no worked pair of colours
/// to check it against, so it is checked on the two pairs the sum itself fixes: the two ends of its scale.
/// </summary>
public class ContrastTests
{
    /// <summary>Black on white is the most two colours can stand apart: 21 to 1.</summary>
    [Test]
    public void BlackOnWhite_IsTwentyOneToOne() =>
        Assert.That(Contrast.Between("#000000", "#ffffff"), Is.EqualTo(21).Within(0.001));

    /// <summary>A colour on itself doesn't stand apart at all: 1 to 1.</summary>
    [Test]
    public void AColourOnItself_IsOneToOne() =>
        Assert.That(Contrast.Between("#6b4dff", "#6b4dff"), Is.EqualTo(1).Within(0.001));

    /// <summary>
    /// A colour Sass wrote from a mix, such as glass's light page, comes as <c>rgb()</c> in percentages, and reads the
    /// same as its <c>#rrggbb</c> form.
    /// </summary>
    [Test]
    public void AColourInPercentages_ReadsAsItsHexForm() =>
        Assert.That(Contrast.Between("rgb(100%, 100%, 100%)", "rgb(0%, 0%, 0%)"), Is.EqualTo(21).Within(0.001));

    /// <summary>Which colour is the words and which the ground makes no difference.</summary>
    [Test]
    public void TheOrder_MakesNoDifference() =>
        Assert.That(Contrast.Between("#ffffff", "#000000"), Is.EqualTo(Contrast.Between("#000000", "#ffffff")));
}
