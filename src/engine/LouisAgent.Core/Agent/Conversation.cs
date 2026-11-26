using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Door;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent;

/// <summary>
/// One chat with the agent: everything said in it so far, starting with the system prompt, and the tools it was
/// started with. A host keeps one per chat and hands it to every turn; it can read the messages and add to them,
/// never change one already there, the system prompt included: skills reloaded mid-chat reach the next chat.
/// </summary>
/// <remarks>
/// Encapsulate Collection (Fowler). The history used to be a bare list every host held and anyone could edit, and the
/// model checks its earlier thinking against everything sent before it, the tools included, so an edit can break a
/// chat. Owning the list here makes adding the only way in, and gives what belongs to a chat, its tools too, one place
/// to live.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
public sealed class Conversation
{
    /// <summary>What has been said, in order.</summary>
    private readonly List<ChatMessage> _messages;

    /// <summary>
    /// Starts a chat with its opening messages: the system prompt, and the no-tools notice if there is one.
    /// </summary>
    internal Conversation(IEnumerable<ChatMessage> opening) => _messages = [.. opening];

    /// <summary>Everything said so far, the system prompt first.</summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>The tools every request of this chat sends: the engine's, as they were at its first turn.</summary>
    internal IReadOnlyList<AITool>? Tools { get; private set; }

    /// <summary>
    /// The chat's tools, copied from the engine's live list at its first turn and kept from then on, each as it was
    /// sent, so a tool made, approved or rejected mid-chat reaches the next chat instead, and a rejected one never
    /// runs.
    /// </summary>
    /// <param name="live">The engine's tools now.</param>
    /// <param name="stillOffered">Whether the engine still offers a tool, by name.</param>
    internal IReadOnlyList<AITool> FreezeTools(IEnumerable<AITool> live, Func<string, bool> stillOffered) =>
        Tools ??= [.. live.Select(tool => Frozen(tool, stillOffered))];

    /// <summary>
    /// A function kept as it was sent; any other kind of tool has nothing that changes, and stays itself.
    /// </summary>
    private static AITool Frozen(AITool tool, Func<string, bool> stillOffered) =>
        tool is AIFunction function ? new FrozenTool(function, stillOffered) : tool;

    /// <summary>The skills this chat drafted, by name.</summary>
    private readonly HashSet<string> _drafts = new(StringComparer.Ordinal);

    /// <summary>
    /// The names of the skills this chat drafted, which only it may use until one is approved (F97.1); the library
    /// finds a draft for a chat only by these, so another chat's draft is never reached.
    /// </summary>
    public IReadOnlySet<string> Drafts => _drafts;

    /// <summary>Notes a skill this chat drafted; drafting it again changes nothing here.</summary>
    internal void Drafted(string name) => _drafts.Add(name);

    /// <summary>What changed outside the model's view since its last turn, to tell it on the next.</summary>
    private readonly List<string> _notes = [];

    /// <summary>
    /// Keeps a note for the model's next turn (F97.1): what the person did outside the chat, such as /approve, which
    /// the host answers itself.
    /// </summary>
    internal void Note(string note) => _notes.Add(note);

    /// <summary>
    /// The person's words, after a system-reminder of the notes kept since the last turn, as Claude Code tells its
    /// model what changed outside its view; each note is told once. Appended as part of the new message, so nothing
    /// already sent changes (ADR 0008).
    /// </summary>
    internal string WithNotes(string userInput)
    {
        if (_notes.Count == 0) return userInput;

        var notes = new SystemReminderHoles(string.Join('\n', _notes));
        _notes.Clear();
        return MessageText.Format(SkillToolMessage.SystemReminder, notes) + userInput;
    }

    /// <summary>The mark of the tools this chat sends; none before its first turn.</summary>
    public ToolsFingerprint? ToolsFingerprint => Tools is null ? null : ToolsFingerprint.Of(Tools);

    /// <summary>
    /// Whether this chat was brought back on tools other than those its saved turns were sent with: a release changed
    /// them since, so the thinking in those turns may no longer match.
    /// </summary>
    public bool ToolsChangedSinceSaved { get; private set; }

    /// <summary>
    /// Takes today's tools for a chat brought back from the store, and notes whether they differ from those its saved
    /// turns were sent with; a chat saved before any turn has nothing to differ from.
    /// </summary>
    internal void ResumeOn(IEnumerable<AITool> today, Func<string, bool> stillOffered, ToolsFingerprint? saved)
    {
        FreezeTools(today, stillOffered);
        ToolsChangedSinceSaved = saved is not null && saved != ToolsFingerprint;
    }

    /// <summary>Adds one message at the end.</summary>
    public void Add(ChatMessage message) => _messages.Add(message);

    /// <summary>Adds messages at the end, in order, such as a turn brought back from the store.</summary>
    public void AddRange(IEnumerable<ChatMessage> messages) => _messages.AddRange(messages);

    /// <summary>Adds a streamed reply at the end, its updates joined into whole messages.</summary>
    internal void AddMessages(IEnumerable<ChatResponseUpdate> updates) => _messages.AddMessages(updates);
}
