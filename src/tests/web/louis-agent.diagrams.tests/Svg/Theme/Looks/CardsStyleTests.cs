using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// What a cards picture's look adds to the shared one (F134.9): the light behind a headline, the lit rail, the
/// glass card and the quiet one, in the palette's own violet running to blue, on a light page and a dark one.
/// </summary>
public partial class CardsStyleTests
{
    private static readonly string Style = CardsStyle.Rules;

    /// <summary>
    /// A variable of the cards' own that a rule reads. It finds <c>crd-sheet</c> in "var(--crd-sheet)", and
    /// nothing in "var(--quiet)", which is the theme's.
    /// </summary>
    [GeneratedRegex(@"var\(--(crd-[a-z-]+)\)", RegexOptions.NonBacktracking)]
    private static partial Regex Read();

    /// <summary>
    /// A variable a block sets. It finds <c>crd-sheet</c> in "--crd-sheet: #ffffff;", and nothing in
    /// "var(--crd-sheet)", which reads it.
    /// </summary>
    [GeneratedRegex(@"--(crd-[a-z-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Set();

    /// <summary>The variables set between a block's opening words and the brace that closes it.</summary>
    private static HashSet<string> SetIn(string opening)
    {
        string block = Style[(Style.IndexOf(opening, StringComparison.Ordinal) + opening.Length)..];
        return [.. Set().Matches(block[..block.IndexOf('}')]).Select(match => match.Groups[1].Value)];
    }

    /// <summary>The style has a rule for every part of the picture the drawing will draw.</summary>
    [TestCase(".crd-title")]
    [TestCase(".crd-light")]
    [TestCase(".crd-rail")]
    [TestCase(".crd-rail-glow")]
    [TestCase(".crd-line")]
    [TestCase(".crd-node")]
    [TestCase(".crd-arrow")]
    [TestCase(".crd-badge")]
    [TestCase(".crd-badge-icon")]
    [TestCase(".crd-headline")]
    [TestCase(".crd-heading")]
    [TestCase(".crd-lede")]
    [TestCase(".crd-pill")]
    [TestCase(".crd-code")]
    [TestCase(".crd-glass")]
    [TestCase(".crd-edge")]
    [TestCase(".crd-tint")]
    [TestCase(".crd-icon")]
    [TestCase(".crd-name")]
    [TestCase(".crd-text")]
    [TestCase(".crd-quiet")]
    public void TheStyle_HasARuleForEveryPart(string rule) => Assert.That(Style, Does.Contain(rule + " "));

    /// <summary>
    /// Every variable of its own that a rule reads is set for a light page and again for a dark one, so no part
    /// falls back to black on either.
    /// </summary>
    [Test]
    public void BothSchemes_SetEveryVariableARuleReads()
    {
        var read = Read().Matches(Style).Select(match => match.Groups[1].Value).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.Not.Empty);
            Assert.That(read.Except(SetIn("svg {")), Is.Empty, "not set for a light page");
            Assert.That(read.Except(SetIn("@media (prefers-color-scheme: dark) {")), Is.Empty,
                "not set for a dark page");
        });
    }

    /// <summary>
    /// The accent is the palette's own violet running to blue, so it has a light and a dark value as every tone
    /// has, and the style names no colour of its own for it.
    /// </summary>
    [Test]
    public void TheAccent_IsThePalettesViolet_RunningToBlue() =>
        Assert.That(Style, Does.Contain("var(--violet-line)").And.Contain("var(--blue-line)"));

    /// <summary>
    /// Each fade the look has, the light, the rail, the badge, a card's top edge and the glass, is a paint the
    /// style fills with by its id and whose stops it colours. The drawing defines a paint under the same id.
    /// </summary>
    [TestCase(CardsStyle.LightPaint, 3)]
    [TestCase(CardsStyle.RailPaint, 4)]
    [TestCase(CardsStyle.BadgePaint, 2)]
    [TestCase(CardsStyle.EdgePaint, 3)]
    [TestCase(CardsStyle.GlassPaint, 2)]
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
    /// The cards' rules name nothing the shared look already has, and the shared look names nothing of a card's.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Not.Contain(".pic-"), "nothing shared written again for cards");
        Assert.That(PictureLook.Of(SvgTheme.Site), Does.Not.Contain(".crd-"), "nothing of a card in the shared look");
    });
}
