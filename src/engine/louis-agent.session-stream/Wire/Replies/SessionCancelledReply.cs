namespace LouisAgent.SessionStream.Wire.Replies;

/// <summary>A cancel: whether a turn was running to cancel.</summary>
public sealed record SessionCancelledReply(string SessionId, bool Cancelled);
