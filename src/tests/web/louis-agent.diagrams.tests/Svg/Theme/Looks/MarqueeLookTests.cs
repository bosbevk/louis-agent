using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;
using LouisAgent.Diagrams.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// What a cards picture and a sequence both wear (F134.10): the small title, the light, the glass with its accent
/// edge, the pill with its code and a node, in one place, so the two kinds can't drift apart in what they share.
/// </summary>
public partial class MarqueeLookTests
{
    private static readonly string Shared = MarqueeLook.Rules;

    /// <summary>
    /// A variable of the look's own that a rule reads. It finds <c>mrq-sheet</c> in "var(--mrq-sheet)", and nothing
    /// in "var(--quiet)", which is the theme's.
    /// </summary>
    [GeneratedRegex(@"var\(--(mrq-[a-z-]+)\)", RegexOptions.NonBacktracking)]
    private static partial Regex Read();

    /// <summary>
    /// A variable a block sets. It finds <c>mrq-sheet</c> in "--mrq-sheet: #ffffff;", and nothing in
    /// "var(--mrq-sheet)", which reads it.
    /// </summary>
    [GeneratedRegex(@"--(mrq-[a-z-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Set();

    /// <summary>The variables set between a block's opening words and the brace that closes it.</summary>
    private static HashSet<string> SetIn(string opening)
    {
        string block = Shared[(Shared.IndexOf(opening, StringComparison.Ordinal) + opening.Length)..];
        return [.. Set().Matches(block[..block.IndexOf('}')]).Select(match => match.Groups[1].Value)];
    }

    /// <summary>The shared look has a rule for each part both kinds draw.</summary>
    [TestCase(".mrq-title")]
    [TestCase(".mrq-light")]
    [TestCase(".mrq-glass")]
    [TestCase(".mrq-edge")]
    [TestCase(".mrq-pill")]
    [TestCase(".mrq-code")]
    [TestCase(".mrq-node")]
    public void TheSharedLook_HasARuleForEachSharedPart(string rule) => Assert.That(Shared, Does.Contain(rule + " "));

    /// <summary>
    /// Every variable of the look's own that a rule reads, here or in a kind's own style, is set for a light page
    /// and again for a dark one.
    /// </summary>
    [Test]
    public void BothSchemes_SetEveryVariableAnyRuleReads()
    {
        string every = Shared + CardsStyle.Rules + SequenceStyle.Of(SvgTheme.Site) +
                       ClassesStyle.Of(SvgTheme.Site);
        var read = Read().Matches(every).Select(match => match.Groups[1].Value).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.Not.Empty);
            Assert.That(read.Except(SetIn("svg {")), Is.Empty, "not set for a light page");
            Assert.That(read.Except(SetIn("@media (prefers-color-scheme: dark) {")), Is.Empty,
                "not set for a dark page");
        });
    }

    /// <summary>
    /// The light, the glass and the accent edge are paints the look fills with by id and whose stops it colours.
    /// </summary>
    [TestCase(MarqueeLook.LightPaint, 3)]
    [TestCase(MarqueeLook.GlassPaint, 2)]
    [TestCase(MarqueeLook.EdgePaint, 3)]
    public void AFade_IsAPaintTheLookFillsWith_AndItsStopsAreColoured(string paint, int stops)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Shared, Does.Contain($"fill: url(#{paint})"));
            for (int stop = 0; stop < stops; stop++)
                Assert.That(Shared, Does.Contain($".{paint}-{stop} {{ stop-color:"), $"stop {stop}");
        });
    }

    /// <summary>
    /// Each rule is written in one place: the shared look names nothing of a card's, of a sequence's or of every
    /// picture's, and the cards' own style names nothing that moved here.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() => Assert.Multiple(() =>
    {
        Assert.That(Shared, Does.Not.Contain(".crd-").And.Not.Contain(".seq-").And.Not.Contain(".pic-"));
        Assert.That(CardsStyle.Rules, Does.Not.Contain(".mrq-"), "nothing shared written again for cards");
        Assert.That(PictureLook.Of(SvgTheme.Site), Does.Not.Contain(".mrq-"));
    });
}
