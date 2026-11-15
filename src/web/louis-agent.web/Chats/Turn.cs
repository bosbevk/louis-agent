using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Chats;

/// <summary>One user message and the agent's streamed answer to it.</summary>
public sealed class Turn
{
    public string UserText { get; init; } = "";
    public List<string> AttachmentNames { get; init; } = [];
    public List<Block> Blocks { get; init; } = [];
    public string? StopReason { get; set; }
    public string? Error { get; set; }

    /// <summary>The toolsets the session loaded, from <c>message_start</c> (F34); null from an older API.</summary>
    public List<string>? Toolsets { get; set; }

    /// <summary>
    /// The session's role, from <c>message_start</c> (FX17): on a specialist's sub-turn its role, on the chat's own
    /// turn its mode; null without one, or from an older API.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>What the turn's model requests used, from <c>message_delta.usage</c>; null for commands.</summary>
    public TurnUsage? Usage { get; set; }

    /// <summary>
    /// The answer's words are what came last (F134.3): the turn ends in text, not in thinking or a tool call. The
    /// agent's orb speaks while they arrive.
    /// </summary>
    public bool EndsInWords => Blocks is [.., { Kind: BlockKind.Text, Text.Length: > 0 }];

    /// <summary>The agent showed a place on the page in this turn (F134.4): its answer then stands beside it.</summary>
    public bool ShowedAPlace => Blocks.Any(block => ShowPageCall.PathOf(block) is not null);

    /// <summary>What the agent said in this turn, its text blocks in order, a blank line between (F134.4).</summary>
    public string Words => string.Join("\n\n",
        Blocks.Where(block => block is { Kind: BlockKind.Text, Text.Length: > 0 }).Select(block => block.Text));
}
