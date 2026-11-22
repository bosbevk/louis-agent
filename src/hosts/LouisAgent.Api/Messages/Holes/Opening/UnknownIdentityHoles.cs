namespace LouisAgent.Api.Messages.Holes.Opening;

/// <summary>An identity a session named that this instance doesn't run, and the ones it does.</summary>
public sealed record UnknownIdentityHoles(string Name, string Available);
