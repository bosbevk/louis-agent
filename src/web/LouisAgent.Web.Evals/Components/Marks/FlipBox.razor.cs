using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Marks;

/// <summary>
/// The box that flips a criterion's mark: it asks for the other answer, takes a one-line note if the person wants to
/// give one, and hands both up. It sends nothing itself; the page does, then reads the run again (ADR 0042).
/// </summary>
/// <remarks>
/// A part of the page under its mark, not a dialog: a flip is put back with one press, so it asks with no modal
/// (Nielsen Norman Group: confirm only what has serious consequences). State down, events up: the note is the box's
/// own until it is handed up.
/// </remarks>
/// <seealso href="https://www.nngroup.com/articles/confirmation-dialog/">NN/g: confirmation dialogs</seealso>
public partial class FlipBox
{
    private string _note = "";

    /// <summary>Whether the flip makes the mark met, which names the question and the button.</summary>
    [Parameter, EditorRequired]
    public bool ToMet { get; set; }

    /// <summary>Hands up the note, or none when it was left empty.</summary>
    [Parameter]
    public EventCallback<string?> OnFlip { get; set; }

    /// <summary>Closes the box, sending nothing.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    private string FlipTo => EvalsWords.Words[ToMet ? EvalsPart.FlipToMet : EvalsPart.FlipToNotMet];

    private static string Counts => EvalsWords.Words[EvalsPart.FlipCounts];

    private static string NoteLabel => EvalsWords.Words[EvalsPart.FlipNoteLabel];

    private static string DontFlip => EvalsWords.Words[EvalsPart.DontFlip];

    /// <summary>The note's field's id, its own so a label points at it.</summary>
    private string NoteId { get; } = "flip-note-" + Guid.NewGuid().ToString("N");

    private void OnNote(ChangeEventArgs typed) => _note = typed.Value?.ToString() ?? "";

    private Task FlipAsync() => OnFlip.InvokeAsync(string.IsNullOrWhiteSpace(_note) ? null : _note.Trim());
}
