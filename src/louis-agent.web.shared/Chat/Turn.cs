namespace LouisAgent.Web.Shared.Chat;

using LouisAgent.Web.Shared.Usage;

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
    /// The session's role, from <c>message_start</c> (FX17): on a specialist's sub-turn its role (<c>puls-cook</c>), on the
    /// chat's own turn its mode (<c>puls/voice-af</c>); null without one, or from an older API.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>What the turn's model requests used, from <c>message_delta.usage</c>; null for commands and older chats.</summary>
    public TurnUsage? Usage { get; set; }
}
