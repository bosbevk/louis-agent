using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a cards picture's look adds to the shared ones (F134.9): the lit rail and its lines, a headline with its
/// badge, a glass card's badge, name and text, and the quiet card, in the palette's own violet running to blue.
/// What a sequence wears too is the shared look's (F134.10), and is held by its own tests.
/// </summary>
public class CardsStyleTests
{
    private static readonly string Style = CardsStyle.Rules;

    /// <summary>The style has a rule for every part that is a cards picture's own.</summary>
    [TestCase(".crd-rail")]
    [TestCase(".crd-rail-glow")]
    [TestCase(".crd-line")]
    [TestCase(".crd-badge")]
    [TestCase(".crd-badge-icon")]
    [TestCase(".crd-headline")]
    [TestCase(".crd-heading")]
    [TestCase(".crd-lede")]
    [TestCase(".crd-tint")]
    [TestCase(".crd-icon")]
    [TestCase(".crd-name")]
    [TestCase(".crd-quiet")]
    public void TheStyle_HasARuleForEveryPartOfItsOwn(string rule) => Assert.That(Style, Does.Contain(rule + " "));

    /// <summary>
    /// The accent is the palette's own violet running to blue, so it has a light and a dark value as every tone
    /// has, and the style names no colour of its own for it.
    /// </summary>
    [Test]
    public void TheAccent_IsThePalettesViolet_RunningToBlue() =>
        Assert.That(Style, Does.Contain("var(--violet-line)").And.Contain("var(--blue-line)"));

    /// <summary>
    /// The rail's fade and the badge's are paints the style fills with by id and whose stops it colours. The
    /// drawing defines a paint under the same id.
    /// </summary>
    [TestCase(CardsStyle.RailPaint, 4)]
    [TestCase(CardsStyle.BadgePaint, 2)]
    public void AFade_IsAPaintTheStyleFillsWith_AndItsStopsAreColoured(string paint, int stops)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Style, Does.Contain($"fill: url(#{paint})"));
            for (int stop = 0; stop < stops; stop++)
                Assert.That(Style, Does.Contain($".{paint}-{stop} {{ stop-color:"), $"stop {stop}");
        });
    }

    /// <summary>
    /// The cards' rules name nothing a shared look already has, set no variable of their own, and every picture's
    /// look names nothing of a card's.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Not.Contain(".pic-").And.Not.Contain(".mrq-"));
        Assert.That(Style, Does.Not.Contain("--crd-"), "its variables are the shared look's");
        Assert.That(PictureLook.Of(SvgTheme.Site), Does.Not.Contain(".crd-"),
            "nothing of a card in every picture's look");
    });
}
