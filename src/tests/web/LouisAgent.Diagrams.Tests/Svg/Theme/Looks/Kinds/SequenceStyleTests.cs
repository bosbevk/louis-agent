using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a sequence's look adds to the shared ones (F134.10): a participant's badge in its tone and its name, the
/// lifeline, a call's beam either way, an answer's quiet line, the words above them, and a block's tab.
/// </summary>
public class SequenceStyleTests
{
    private static readonly string Style = SequenceStyle.Of(SvgTheme.Site);

    /// <summary>The style has a rule for every part that is a sequence's own.</summary>
    [TestCase(".seq-badge")]
    [TestCase(".seq-name")]
    [TestCase(".seq-life")]
    [TestCase(".seq-beam")]
    [TestCase(".seq-beam-glow")]
    [TestCase(".seq-tip")]
    [TestCase(".seq-turn")]
    [TestCase(".seq-answer")]
    [TestCase(".seq-call-words")]
    [TestCase(".seq-answer-words")]
    [TestCase(".seq-cover")]
    [TestCase(".seq-kind")]
    [TestCase(".seq-tab")]
    [TestCase(".seq-divide")]
    [TestCase(".seq-else")]
    [TestCase(".seq-else-word")]
    [TestCase(".seq-count")]
    [TestCase(".seq-quiet-node")]
    [TestCase(".seq-swatch")]
    public void TheStyle_HasARuleForEveryPartOfItsOwn(string rule) => Assert.That(Style, Does.Contain(rule + " "));

    /// <summary>
    /// A badge is filled with its participant's tone, so a tone says here what it says in a flow, and a
    /// participant with no tone has a quiet badge.
    /// </summary>
    [TestCase("blue")]
    [TestCase("green")]
    [TestCase("violet")]
    [TestCase("teal")]
    [TestCase("red")]
    [TestCase("amber")]
    public void ABadge_IsFilledWithItsTone(string tone) => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Contain($".seq-badge.tone-{tone} {{ fill: var(--{tone}-line); }}"));
        Assert.That(Style, Does.Contain(".seq-badge { fill: var(--quiet); }"), "with no tone, quiet");
    });

    /// <summary>
    /// A call's beam runs from violet at its sender to blue at its receiver, whichever way it points: one paint
    /// for a call to the right and its mirror for a call to the left. A lifeline fades out down the picture.
    /// </summary>
    [Test]
    public void ABeam_RunsFromVioletAtItsSenderToBlue_EitherWay() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Contain($".{SequenceStyle.BeamPaint}-0 {{ stop-color: var(--violet-line); }}"));
        Assert.That(Style, Does.Contain($".{SequenceStyle.BeamPaint}-1 {{ stop-color: var(--blue-line); }}"));
        Assert.That(Style, Does.Contain($".{SequenceStyle.BackPaint}-0 {{ stop-color: var(--blue-line); }}"));
        Assert.That(Style, Does.Contain($".{SequenceStyle.BackPaint}-1 {{ stop-color: var(--violet-line); }}"));
        Assert.That(Style, Does.Contain($".seq-beam {{ fill: url(#{SequenceStyle.BeamPaint}); }}"));
        Assert.That(Style, Does.Contain($".seq-beam.back {{ fill: url(#{SequenceStyle.BackPaint}); }}"));
        Assert.That(Style, Does.Contain($".seq-life {{ fill: url(#{SequenceStyle.LifePaint}); }}"));
        Assert.That(Style,
            Does.Contain($".{SequenceStyle.LifePaint}-2 {{ stop-color: var(--blue-line); stop-opacity: 0; }}"));
    });

    /// <summary>
    /// A message's words and a block's label have a halo of the ground's colour round their letters, so a
    /// lifeline that runs behind them doesn't show through.
    /// </summary>
    [TestCase(".seq-call-words")]
    [TestCase(".seq-answer-words")]
    [TestCase(".seq-tab")]
    public void WordsOverALifeline_HaveAHaloOfTheGround(string rule)
    {
        string body = Style[Style.IndexOf(rule + " {", StringComparison.Ordinal)..];

        Assert.That(body[..body.IndexOf('}')],
            Does.Contain("paint-order: stroke").And.Contain("stroke: var(--ground)"));
    }

    /// <summary>
    /// The sequence's rules name nothing a shared look already has, and set no variable of their own: the few it
    /// reads are the shared look's and the theme's.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Not.Contain(".pic-").And.Not.Contain(".mrq-").And.Not.Contain(".crd-"));
        Assert.That(Style, Does.Not.Contain("--seq-"), "its variables are the shared look's");
        Assert.That(CardsStyle.Rules, Does.Not.Contain(".seq-"));
    });

    /// <summary>What a sequence drawn as steps for a phone adds (F169.7), asked for by that drawing alone.</summary>
    private static readonly string StepList = SequenceStyle.StepList(SvgTheme.Site);

    /// <summary>The step list has a rule for every part of its own, and the wide style none of them.</summary>
    [TestCase(".stl-bar")]
    [TestCase(".stl-bar.quiet")]
    [TestCase(".stl-dot")]
    [TestCase(".stl-name")]
    [TestCase(".stl-words")]
    [TestCase(".stl-words.quiet")]
    public void TheStepList_HasARuleForEveryPartOfItsOwn(string rule) => Assert.Multiple(() =>
    {
        Assert.That(StepList, Does.Contain(rule + " "));
        Assert.That(Style, Does.Not.Contain(".stl-"), "so a picture in the docs is byte for byte as it was");
    });

    /// <summary>A step's square is filled with its participant's tone; with no tone, it is quiet.</summary>
    [TestCase("blue")]
    [TestCase("green")]
    [TestCase("violet")]
    [TestCase("teal")]
    [TestCase("red")]
    [TestCase("amber")]
    public void AStepsSquare_IsFilledWithItsTone(string tone) => Assert.Multiple(() =>
    {
        Assert.That(StepList, Does.Contain($".stl-dot.tone-{tone} {{ fill: var(--{tone}-line); }}"));
        Assert.That(StepList, Does.Contain(".stl-dot { fill: var(--quiet); }"), "with no tone, quiet");
    });

    /// <summary>
    /// No part of the step list is filled with a fade or passed through a filter, so the flat page's rules need none
    /// for it, and its words are 10 pixels or more.
    /// </summary>
    [Test]
    public void NoPartOfTheStepList_IsShaded_AndNoWordsAreSmall() => Assert.Multiple(() =>
    {
        Assert.That(StepList, Does.Not.Contain("url(#"));
        Assert.That(StepList, Does.Not.Contain("filter:"));
        Assert.That(StepList, Does.Contain("12.5px"));
        Assert.That(StepList, Does.Not.Match(@"(?<![\d.])[1-9](\.\d+)?px var"));
    });
}
