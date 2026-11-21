namespace LouisAgent.SessionStream.Wire.Replies;

/// <summary>A session that was deleted.</summary>
public sealed record SessionDeletedReply(string SessionId, bool Deleted);
