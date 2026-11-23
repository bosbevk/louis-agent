using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Marks;

/// <summary>
/// One mark of a turn. A fact is checked in code, so it reads as code; a criterion is the judge's, so it carries the
/// judge's line of why and a Flip, which opens the box under it. A flipped mark says what it was flipped to, what the
/// judge said and the note, and puts the judge's answer back with one press.
/// </summary>
/// <remarks>
/// State down, events up: the line holds only whether its box is open, and hands a flip or an undo up to the page,
/// which sends it and reads the run again, so the mark shown is always the one the run keeps (ADR 0042). Its tick
/// is the answer that counts, the mark's own <see cref="Mark.CountsAsMet" /> (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class MarkLine
{
    private bool _asking;

    /// <summary>The mark.</summary>
    [Parameter, EditorRequired]
    public Mark Mark { get; set; } = null!;

    /// <summary>Whether it is a fact or a criterion.</summary>
    [Parameter, EditorRequired]
    public MarkKind Kind { get; set; }

    /// <summary>Where a criterion's mark is in its run, so it can be flipped; none draws no Flip.</summary>
    [Parameter]
    public MarkPlace? Place { get; set; }

    /// <summary>Hands up a flip of this mark, with its note.</summary>
    [Parameter]
    public EventCallback<FlipAsk> OnFlip { get; set; }

    /// <summary>Hands up an undo of this mark's flip.</summary>
    [Parameter]
    public EventCallback<MarkPlace> OnUndo { get; set; }

    /// <summary>The eval's runs, so a flipped mark says how often it was flipped lately (F126.4); or none.</summary>
    [Parameter]
    public RunHistory? History { get; set; }

    /// <summary>
    /// "You flipped this criterion in 2 of the last 8 runs. A criterion you keep flipping…": its runs' own tallies,
    /// said only once it was flipped in one.
    /// </summary>
    private string? Lately
    {
        get
        {
            if (History is null) return null;
            if (History.TallyMissedMost(RunHistory.Recent).FirstOrDefault(tally => tally.Text == Mark.Text) is not
                { Flipped: > 0 } tally) return null;

            int runs = History.FindLastFinished(RunHistory.Recent).Count;
            return EvalsWords.Fill(EvalsPart.FlippedLately, tally.Flipped, runs) + " " +
                   EvalsWords.Words[EvalsPart.KeepFlipping];
        }
    }

    private string KindClass => Kind == MarkKind.Fact ? "mark-line-fact" : "mark-line-criterion";

    private string MetClass => Mark.CountsAsMet() ? "mark-line-met" : "mark-line-not-met";

    private bool Flipped => Mark.Flip is not null;

    private static string FactInCode => EvalsWords.Words[EvalsPart.FactInCode];

    private static string FlipWord => EvalsWords.Words[EvalsPart.Flip];

    private static string UndoTheFlip => EvalsWords.Words[EvalsPart.UndoTheFlip];

    private string TheJudge => EvalsWords.Fill(EvalsPart.TheJudge, Mark.Reason);

    private string FlippedTo => EvalsWords.Fill(EvalsPart.FlippedTo, MetWords(!Mark.Met));

    private string JudgeSaid => EvalsWords.Fill(EvalsPart.JudgeSaid, MetWords(Mark.Met), Mark.Reason);

    private static string NoteOf(string note) => EvalsWords.Fill(EvalsPart.YourNote, note);

    private static string MetWords(bool met) => EvalsWords.Words[met ? EvalsPart.Met : EvalsPart.NotMet];

    private void Open() => _asking = true;

    private void Close() => _asking = false;

    /// <summary>Closes the box and hands the flip up.</summary>
    private Task FlipAsync(string? note)
    {
        _asking = false;
        var ask = new FlipAsk(Place!, note);
        return OnFlip.InvokeAsync(ask);
    }

    private Task UndoAsync() => OnUndo.InvokeAsync(Place!);
}
