namespace LouisAgent.Acp.Messages.Holes.UserText;

/// <summary>The protocol version Rider asked for, when it isn't the one this server speaks.</summary>
public sealed record UnsupportedProtocolHoles(int Version);
