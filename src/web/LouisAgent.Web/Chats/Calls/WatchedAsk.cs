namespace LouisAgent.Web.Chats.Calls;

/// <summary>
/// The guide's ask to a business, watched in that business's window (F127.4.2): the visitor's words and the business's
/// answer as one turn of a chat on the business's own session, its blocks shared with the guide's stream so the window
/// follows it as it comes, and the business's tool calls handed on once each.
/// </summary>
/// <remarks>
/// The window's runner takes this chat as its own (its <c>Chat</c> is settable), so once the answer ends the visitor's
/// next message goes to the same session (Louis's pick L2) with no new runner method; the stored history is the
/// business's own, on the server.
/// </remarks>
public sealed class WatchedAsk(Block call)
{
    /// <summary>How many of the business's blocks were already handed on.</summary>
    private int _seen;

    /// <summary>The guide's ask.</summary>
    public Block Call => call;

    /// <summary>Whether the business has answered: the guide's call has its result.</summary>
    public bool Done => call.ToolResult is not null;

    /// <summary>The chat on the business's session, once its session and its answer have begun; none before.</summary>
    public Chat? Chat { get; private set; }

    /// <summary>
    /// Builds the chat the first time the business's session and answer are both known, and gives back the tool calls
    /// the business made since the last look.
    /// </summary>
    public IReadOnlyList<Block> Look()
    {
        if (call.SubSessionId is not { } session || call.SubTurn is not { } answer) return [];
        Chat ??= new Chat
        {
            SessionId = session,
            Title = Delegation.From(call)?.Task ?? "",
            Turns =
            [
                new Turn
                {
                    UserText = Delegation.From(call)?.Task ?? "",
                    Blocks = answer.Blocks
                }
            ]
        };

        var calls = answer.Blocks.Skip(_seen).Where(block => block.Kind == BlockKind.Tool).ToList();
        _seen = answer.Blocks.Count;
        return calls;
    }
}
