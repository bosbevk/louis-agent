namespace LouisAgent.Acp.Protocol;

/// <summary>Our answer to <c>session/new</c>: the id Rider uses for the chat from then on.</summary>
internal sealed record NewSessionReply(string SessionId);
