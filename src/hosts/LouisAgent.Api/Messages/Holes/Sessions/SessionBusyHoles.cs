namespace LouisAgent.Api.Messages.Holes.Sessions;

/// <summary>The session that is still answering, named in the address to cancel it.</summary>
public sealed record SessionBusyHoles(string Id);
