using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a sequence's list of steps adds to the sequence's look (F169.7): a step's bar, a participant's square in
/// its tone, its name and a step's words, all in plain colours, so a flat page has nothing of it to hide.
/// </summary>
public class StepListStyleTests
{
    private static readonly string Style = StepListStyle.Of(SvgTheme.Site);

    /// <summary>The style has a rule for every part that is a list of steps' own.</summary>
    [TestCase(".stl-bar")]
    [TestCase(".stl-bar.quiet")]
    [TestCase(".stl-dot")]
    [TestCase(".stl-name")]
    [TestCase(".stl-words")]
    [TestCase(".stl-words.quiet")]
    public void TheStyle_HasARuleForEveryPartOfItsOwn(string rule) => Assert.That(Style, Does.Contain(rule + " "));

    /// <summary>A square is filled with its participant's tone; a participant with no tone has a quiet one.</summary>
    [TestCase("blue")]
    [TestCase("green")]
    [TestCase("violet")]
    [TestCase("teal")]
    [TestCase("red")]
    [TestCase("amber")]
    public void ASquare_IsFilledWithItsTone(string tone) => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Contain($".stl-dot.tone-{tone} {{ fill: var(--{tone}-line); }}"));
        Assert.That(Style, Does.Contain(".stl-dot { fill: var(--quiet); }"), "with no tone, quiet");
    });

    /// <summary>
    /// No part is filled with a fade or passed through a filter, so the flat page's rules need none for it, and its
    /// words are 10 pixels or more.
    /// </summary>
    [Test]
    public void NoPart_IsShaded_AndNoWordsAreSmall() => Assert.Multiple(() =>
    {
        Assert.That(Style, Does.Not.Contain("url(#"));
        Assert.That(Style, Does.Not.Contain("filter:"));
        Assert.That(Style, Does.Contain("12.5px"));
        Assert.That(Style, Does.Not.Match(@"(?<![\d.])[1-9](\.\d+)?px var"));
    });
}
