using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// The look every picture in the new style shares (F134.8): one style under the component diagram and the flow, so
/// what they share can't drift apart, and each kind adds only what is its own. Since F134.12 it is the others'
/// look: a panel with a little body and a hairline, and nothing that glows, shines or tints.
/// </summary>
public partial class PictureLookTests
{
    private static readonly string Shared = PictureLook.Of(SvgTheme.Site);

    /// <summary>
    /// A variable of the look's own that a rule reads. It finds <c>pic-body</c> in "var(--pic-body)", and nothing
    /// in "var(--quiet)", which is the theme's.
    /// </summary>
    [GeneratedRegex(@"var\(--(pic-[a-z0-9-]+)\)", RegexOptions.NonBacktracking)]
    private static partial Regex Read();

    /// <summary>
    /// A variable a block sets. It finds <c>pic-body</c> in "--pic-body: #ffffff;", and nothing in
    /// "var(--pic-body)", which reads it.
    /// </summary>
    [GeneratedRegex(@"--(pic-[a-z0-9-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Set();

    /// <summary>The variables set between a block's opening words and the brace that closes it.</summary>
    private static HashSet<string> SetIn(string opening)
    {
        string block = Shared[(Shared.IndexOf(opening, StringComparison.Ordinal) + opening.Length)..];
        return [.. Set().Matches(block[..block.IndexOf('}')]).Select(match => match.Groups[1].Value)];
    }

    /// <summary>
    /// The shared look carries what any picture in it wears: a panel, a line with its marching dots, and a port.
    /// </summary>
    [TestCase(".pic-panel")]
    [TestCase(".pic-line")]
    [TestCase(".pic-march")]
    [TestCase(".pic-port")]
    public void TheSharedLook_CarriesWhatEveryPictureWears(string rule)
    {
        Assert.That(Shared, Does.Contain(rule + " "));
    }

    /// <summary>
    /// What the look had before the other kinds settled on theirs is gone (F134.12): the ground's dots, a panel's
    /// highlight, the large title, a colour a depth, a panel tinted in its tone, and a shadow under it.
    /// </summary>
    [TestCase(".pic-dots")]
    [TestCase(".pic-sheen")]
    [TestCase(".pic-title")]
    [TestCase("depth-")]
    [TestCase("--pic-tone")]
    [TestCase("--pic-0")]
    [TestCase("drop-shadow")]
    public void TheSharedLook_HasNoRuleForWhatWent(string gone)
    {
        Assert.That(Shared, Does.Not.Contain(gone));
    }

    /// <summary>
    /// A panel is filled with a little of the page's ink and edged with a hairline, the same at every depth, so a
    /// panel inside a panel is a step deeper by itself.
    /// </summary>
    [Test]
    public void APanel_HasBodyAndAHairline()
    {
        Assert.That(Shared,
            Does.Contain(".pic-panel { fill: var(--pic-body); stroke: var(--pic-hair); stroke-width: 1; }"));
    }

    /// <summary>
    /// Every variable of the look's own that a rule reads, here or in a kind's own style, is set for a light page,
    /// and a dark page sets every colour a light page does. The face is the one variable that is no colour.
    /// </summary>
    [Test]
    public void BothSchemes_SetEveryVariableAnyRuleReads()
    {
        string every = Shared + FlowStyle.Rules + ComponentsStyle.Of(SvgTheme.Site);
        var read = Read().Matches(every).Select(match => match.Groups[1].Value).ToHashSet();
        var light = SetIn("svg {");

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.Not.Empty);
            Assert.That(read.Except(light), Is.Empty, "not set for a light page");
            Assert.That(SetIn("@media (prefers-color-scheme: dark) {"), Is.EquivalentTo(light.Except(["pic-sans"])),
                "a dark page sets the same colours");
            Assert.That(light.Except(read), Is.Empty, "set, and read by nothing");
        });
    }

    /// <summary>It carries its colours for a light and a dark page, and stops what moves on request.</summary>
    [Test]
    public void TheSharedLook_HasBothThemesAndStopsOnRequest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Shared, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(Shared, Does.Contain("@media (prefers-reduced-motion: reduce)"));
            Assert.That(Shared, Does.Contain(".pic-march { display: none; }"));
        });
    }

    /// <summary>
    /// The shared look names nothing that is one kind's own, and a component's own rules name none of the shared
    /// ones again, so each rule is written in one place.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace()
    {
        string components = ComponentsStyle.Of(SvgTheme.Site);

        Assert.Multiple(() =>
        {
            Assert.That(Shared, Does.Not.Contain(".cmp-"), "nothing of a component in the shared look");
            Assert.That(components, Does.Not.Contain(".pic-"), "nothing shared written again for a component");
            Assert.That(components, Does.Contain(".cmp-pin").And.Contain(".cmp-pill").And.Contain(".cmp-ghost"));
        });
    }
}
