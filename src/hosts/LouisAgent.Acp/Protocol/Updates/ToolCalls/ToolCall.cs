namespace LouisAgent.Acp.Protocol.Updates.ToolCalls;

/// <summary>A tool call starting: Rider opens a card for it, which later updates finish.</summary>
/// <param name="ToolCallId">The call's id, which its update names again.</param>
/// <param name="Title">The card's title: the tool and what it was asked.</param>
/// <param name="Kind">What sort of work it is (read, edit, execute…), which picks the card's icon.</param>
/// <param name="Status">Where it stands; a new call is running.</param>
/// <param name="RawInput">The arguments the model passed, as they were.</param>
internal sealed record ToolCall(
    string ToolCallId,
    string Title,
    string Kind,
    ToolCallStatus Status,
    IDictionary<string, object?>? RawInput) : SessionUpdate;
