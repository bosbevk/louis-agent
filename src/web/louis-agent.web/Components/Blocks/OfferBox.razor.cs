using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Chats.Calls.Forms;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Blocks;

/// <summary>
/// A choice the agent offered that asks the visitor to write (FX147, Louis: "that first card should have had a input
/// with button"): its box, and Send, ready once something is written. What they wrote goes up after the label, as a
/// form's one field would, so the offer folds to this card once it is answered.
/// </summary>
/// <remarks>
/// State flows down and the send goes up, as in <see cref="OfferCards"/> and <see cref="FormCard"/>; the message is
/// <see cref="FormAnswer"/>'s, the same words a form of one field would send.
/// </remarks>
public partial class OfferBox
{
    private string _words = "";

    /// <summary>The choice, with the kind of box it holds.</summary>
    [Parameter, EditorRequired]
    public OfferChoice Choice { get; set; } = default!;

    /// <summary>The question the choices answer, the form's question when the words are read back.</summary>
    [Parameter, EditorRequired]
    public string Question { get; set; } = "";

    /// <summary>Whether nothing can be sent yet: the answer still streams, or nothing would send it.</summary>
    [Parameter]
    public bool Waiting { get; set; }

    /// <summary>What Send does with the message: the chat sends it.</summary>
    [Parameter]
    public EventCallback<string> Sent { get; set; }

    /// <summary>The box's kind; a plain line when the choice names none.</summary>
    private AskedKind Kind => Choice.Writes ?? AskedKind.Text;

    /// <summary>Send is ready once something is written.</summary>
    private bool Ready => !Waiting && !string.IsNullOrWhiteSpace(_words);

    /// <summary>Keeps what was typed.</summary>
    private void Typed(ChangeEventArgs typed) => _words = typed.Value?.ToString() ?? "";

    /// <summary>Sends the label and what was written as the visitor's message.</summary>
    private Task SendAsync()
    {
        if (!Ready) return Task.CompletedTask;
        var one = OfferWriting.FormOf(Question, Choice);
        return Sent.InvokeAsync(FormAnswer.Of(one, [_words]));
    }
}
