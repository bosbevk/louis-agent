using System.Net.Http.Json;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Notes;

/// <summary>
/// Shows a run's notes, or one conversation's, with a small form to add one. It never adds the note to its own list: it says one was written, and the run page fetches again.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class NotesPanel
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private RunNotesState State { get; set; } = null!;

    [Parameter, EditorRequired] public string RunId { get; set; } = "";
    [Parameter] public IReadOnlyList<RunNoteEntry> Notes { get; set; } = [];
    [Parameter] public ConversationRef? Conversation { get; set; }

    /// <summary>What's typed in the box.</summary>
    private string _text = "";

    /// <summary>Why the last post failed, if it did.</summary>
    private string? _error;

    /// <summary>Whether a post is on its way.</summary>
    private bool _busy;

    /// <summary>This place's notes, newest first.</summary>
    private IReadOnlyList<RunNoteEntry> Shown => NoteList.Newest(Notes, Conversation);

    /// <summary>Nothing but spaces typed yet.</summary>
    private bool Blank => string.IsNullOrWhiteSpace(_text);

    /// <summary>The hint to show.</summary>
    private string Placeholder => NoteText.Placeholder(Conversation is not null);

    /// <summary>Sends the note, clears the box and tells the state. If the API says no, we show why and keep the text.</summary>
    private async Task AddAsync()
    {
        // Nothing to send, or a post is already on its way.
        if (Blank || _busy) return;

        _busy = true;
        _error = null;
        try
        {
            // The text, and the conversation's key when this is a conversation's panel (null for a note on the run).
            var body = new
            {
                text = _text,
                conversation = Conversation is { } c ? new { mode = c.Mode, repeat = c.Repeat, task = c.Task } : null
            };

            // Post it to this run's notes. The run id is escaped because it goes into the web address.
            using var request =
                new HttpRequestMessage(HttpMethod.Post, $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/notes")
                { Content = JsonContent.Create(body) };
            // We only need to know it worked. If the API says no, it throws, and the catch below shows why.
            using var _ = await Api.SendAsync(request);

            // Saved: empty the box, and tell the run page to fetch the notes again (we never add it to the list ourselves).
            _text = "";
            State.Written();
        }
        catch (AgentApiException ex)
        {
            // The API turned it down: show why, and keep the text so it can be sent again.
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
