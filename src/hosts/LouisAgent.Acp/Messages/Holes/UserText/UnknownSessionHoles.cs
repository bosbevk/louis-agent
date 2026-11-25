namespace LouisAgent.Acp.Messages.Holes.UserText;

/// <summary>A session id this server never started, or no longer has.</summary>
public sealed record UnknownSessionHoles(string Session);
