namespace LouisAgent.Acp.Messages.Holes.UserText;

/// <summary>The method Rider asked for that this server doesn't have; none when the message named no method.</summary>
public sealed record MethodNotFoundHoles(string? Method);
