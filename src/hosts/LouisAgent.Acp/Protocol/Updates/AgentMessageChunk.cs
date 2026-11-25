namespace LouisAgent.Acp.Protocol.Updates;

/// <summary>
/// A piece of the answer. Pieces with the same message id join into one message, so text before and after a tool
/// call shows as two.
/// </summary>
internal sealed record AgentMessageChunk(string MessageId, TextBlock Content) : SessionUpdate;
