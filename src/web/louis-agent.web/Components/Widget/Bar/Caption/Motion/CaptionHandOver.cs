using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Components.Widget.Bar.Caption.Motion;

/// <summary>
/// Which of the guide's answers are on their way out (F154, Louis's pick A of three). An answer the bar stops showing,
/// put away or followed by the next, does not vanish: it begins to leave, fading, and only goes once its caption says
/// it has. The bar tells it which answer it shows on each draw, and draws the leaving ones beside it.
/// </summary>
/// <remarks>
/// A small class of its own, so the bar only asks it and the rule is tested apart from any drawing (ADR 0123); the bar
/// keys every caption by its answer, so a leaving caption is the same caption, told it is leaving, not a new one.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed class CaptionHandOver
{
    /// <summary>The answer the bar showed last, if any.</summary>
    private Turn? _shown;

    /// <summary>The answers leaving, oldest first.</summary>
    private readonly List<Turn> _leaving = [];

    /// <summary>The answers on their way out, oldest first.</summary>
    public IReadOnlyList<Turn> Leaving => _leaving;

    /// <summary>The bar shows this answer now, or none: the one it showed before, if another, begins to leave.</summary>
    public void Showing(Turn? turn)
    {
        if (_shown is { } before && !ReferenceEquals(before, turn) && !_leaving.Contains(before)) _leaving.Add(before);
        _shown = turn;
    }

    /// <summary>This answer's caption has faded: it is gone.</summary>
    public void Left(Turn turn) => _leaving.Remove(turn);

    /// <summary>Whether this answer's caption is on its way out.</summary>
    public bool IsLeaving(Turn turn) => _leaving.Contains(turn);

    /// <summary>
    /// The answers to draw as captions, with this one standing now, or none: those leaving, then the standing one. One
    /// no longer standing begins to leave.
    /// </summary>
    public IReadOnlyList<Turn> Draw(Turn? standing)
    {
        Showing(standing);
        return standing is null ? Leaving : [.. _leaving, standing];
    }
}
