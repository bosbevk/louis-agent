namespace LouisAgent.Core.Delegation.Messages.Holes;

/// <summary>An identity asked by its name alone that has no voice, and the subagents it has instead.</summary>
public sealed record AddressNoVoiceHoles(string Identity, string Roles);
