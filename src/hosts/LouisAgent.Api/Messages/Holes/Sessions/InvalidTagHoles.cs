namespace LouisAgent.Api.Messages.Holes.Sessions;

/// <summary>A session's tag that is too long or not printable, and the most it may be.</summary>
public sealed record InvalidTagHoles(string Tag, int Max);
