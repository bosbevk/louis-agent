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
    /// The chat's tools, copied from the engine's live list at its first turn and kept from then on, so a tool made,
    /// approved or rejected mid-chat reaches the next chat instead.
    /// </summary>
    internal IReadOnlyList<AITool> FreezeTools(IEnumerable<AITool> live) => Tools ??= [.. live];

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
    internal void ResumeOn(IEnumerable<AITool> today, ToolsFingerprint? saved)
    {
        FreezeTools(today);
        ToolsChangedSinceSaved = saved is not null && saved != ToolsFingerprint;
    }

    /// <summary>Adds one message at the end.</summary>
    public void Add(ChatMessage message) => _messages.Add(message);

    /// <summary>Adds messages at the end, in order, such as a turn brought back from the store.</summary>
    public void AddRange(IEnumerable<ChatMessage> messages) => _messages.AddRange(messages);

    /// <summary>Adds a streamed reply at the end, its updates joined into whole messages.</summary>
    internal void AddMessages(IEnumerable<ChatResponseUpdate> updates) => _messages.AddMessages(updates);
}
