using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a class chart's look adds to the shared ones (F134.11): a type's badge in its tone with its letter, its
/// name, kind and members, a line for each kind of link with its ends, the word's pill, and the key's parts.
/// </summary>
public class ClassesStyleTests
{
    private static readonly string Style = ClassesStyle.Of(SvgTheme.Site);

    /// <summary>The style has a rule for every part that is a class chart's own.</summary>
    [TestCase(".cls-cover")]
    [TestCase(".cls-group")]
    [TestCase(".cls-badge")]
    [TestCase(".cls-mark")]
    [TestCase(".cls-divide")]
    [TestCase(".cls-member")]
    [TestCase(".cls-line")]
    [TestCase(".cls-word")]
    [TestCase(".cls-key-mark")]
    [TestCase(".cls-key-letter")]
    [TestCase(".end-triangle")]
    [TestCase(".end-diamond")]
    [TestCase(".end-open")]
    public void TheStyle_HasARuleForEveryPartOfItsOwn(string rule) => Assert.That(Style, Does.Contain(rule + " "));

    /// <summary>
    /// A link's kind is in its line: the structure, holds, is a and implements, in violet, a creates in blue and
    /// a use in grey. Implements, creates and uses are broken lines, and holds and is a are whole.
    /// </summary>
    [TestCase("holds", "--violet-line", false)]
    [TestCase("inherits", "--violet-line", false)]
    [TestCase("implements", "--violet-line", true)]
    [TestCase("creates", "--blue-line", true)]
    [TestCase("uses", "--quiet", true)]
    public void ALinksKind_IsInItsLine(string kind, string colour, bool broken)
    {
        string rule = Rule($".cls-line.{kind}");

        Assert.Multiple(() =>
        {
            Assert.That(rule, Does.Contain($"stroke: var({colour})"));
            Assert.That(rule.Contains("stroke-dasharray"), Is.EqualTo(broken));
            Assert.That(Rule($".cls-chip.{kind}"), Does.Contain($"stroke: var({colour})"), "its word's pill too");
        });
    }

    /// <summary>
    /// A badge is filled with its type's tone, and an end takes its line's: a triangle's outline, a diamond's
    /// fill and an open arrowhead's stroke. With no tone each is grey.
    /// </summary>
    [TestCase("violet")]
    [TestCase("teal")]
    [TestCase("green")]
    [TestCase("blue")]
    public void ABadgeAndAnEnd_TakeTheirTone(string tone) => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Contain($".cls-badge.tone-{tone} {{ fill: var(--{tone}-line); }}"));
        Assert.That(Style, Does.Contain($".end-triangle.tone-{tone} {{ stroke: var(--{tone}-line); }}"));
        Assert.That(Style, Does.Contain($".end-diamond.tone-{tone} {{ fill: var(--{tone}-line); }}"));
        Assert.That(Style, Does.Contain($".end-open.tone-{tone} {{ stroke: var(--{tone}-line); }}"));
        Assert.That(Rule(".end-triangle"), Does.Contain("fill: var(--ground)").And.Contain("stroke: var(--quiet)"));
    });

    /// <summary>
    /// The class chart's rules name nothing a shared look or another kind already has, and set no variable of
    /// their own: the few they read are the shared looks' and the theme's.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Not.Contain(".pic-").And.Not.Contain(".mrq-").And.Not.Contain(".crd-").And.Not
            .Contain(".seq-").And.Not.Contain(".flow-"));
        Assert.That(Style, Does.Not.Contain("--cls-"), "its variables are the shared looks'");
        Assert.That(SequenceStyle.Of(SvgTheme.Site) + CardsStyle.Rules, Does.Not.Contain(".cls-"));
    });

    /// <summary>The body of the rule that starts with a selector.</summary>
    private static string Rule(string selector)
    {
        string from = Style[Style.IndexOf(selector + " {", StringComparison.Ordinal)..];
        return from[..from.IndexOf('}')];
    }
}
