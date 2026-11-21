using LouisAgent.Web.Chats.Calls.Forms;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Blocks;

/// <summary>
/// Draws a form the agent asked with: a box per field, each suited to what it asks, and Send, which is ready once
/// every box is filled. What was filled in goes up as the visitor's message; once they have answered, the form folds
/// to what they sent.
/// </summary>
/// <remarks>
/// State flows down and the send goes up (the hosts guide's rule), as in <see cref="OfferCards"/>: the form holds no
/// runner, and the chat that owns one says what Send does through <see cref="Sent"/>, so a recording, which sends
/// nothing, draws it waiting. The message itself is <see cref="FormAnswer"/>'s, a plain class with its own tests.
/// </remarks>
public partial class FormCard
{
    /// <summary>What the visitor has typed so far, a value per field.</summary>
    private string[] _values = [];

    /// <summary>The question and its fields.</summary>
    [Parameter, EditorRequired]
    public Asked Form { get; set; } = default!;

    /// <summary>The visitor's next message, once there is one: the filled form, or their own words.</summary>
    [Parameter]
    public string? Answer { get; set; }

    /// <summary>Whether the answer that asks with this form is still coming in.</summary>
    [Parameter]
    public bool Streaming { get; set; }

    /// <summary>What Send does with the filled form's message: the chat sends it.</summary>
    [Parameter]
    public EventCallback<string> Sent { get; set; }

    /// <summary>The boxes can't be filled while the answer streams, or where nothing would send them.</summary>
    private bool Waiting => Streaming || !Sent.HasDelegate;

    /// <summary>Send is ready once every box holds something.</summary>
    private bool Ready => !Waiting && _values.All(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Each field with where it sits and what has been typed in it.</summary>
    private IEnumerable<(AskedField Field, int At, string Value)> Boxes =>
        Form.Fields.Select((asked, at) => (asked, at, _values[at]));

    /// <summary>
    /// Each field and what the visitor sent in it, once they sent the form; none when they wrote their own.
    /// </summary>
    private IEnumerable<(string Label, string Value)>? SentRows
    {
        get
        {
            if (Answer is null) return null;
            var sent = FormAnswer.Read(Form, Answer);
            return sent is null ? null : Form.Fields.Zip(sent, (asked, value) => (asked.Label, value));
        }
    }

    /// <summary>A box for every field, empty, whenever the form changes.</summary>
    protected override void OnParametersSet()
    {
        if (_values.Length != Form.Fields.Count) _values = new string[Form.Fields.Count];
    }

    /// <summary>Keeps what the visitor typed in one box.</summary>
    private void Typed(int at, ChangeEventArgs typed) => _values[at] = typed.Value?.ToString() ?? "";

    /// <summary>Sends the filled form as the visitor's message.</summary>
    private Task SendAsync() => Ready ? Sent.InvokeAsync(FormAnswer.Of(Form, _values)) : Task.CompletedTask;
}
