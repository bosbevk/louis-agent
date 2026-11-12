namespace LouisAgent.SessionStream.Wire.Replies.Work;

/// <summary>
/// A session's work, step by step, as <c>GET /v1/sessions/{id}/work</c> gives it (F127.4.2): what it was asked, what it
/// said, and each tool it called with what came back, without its thinking.
/// </summary>
public sealed record SessionWorkReply(string SessionId, IReadOnlyList<WorkStep> Steps);
