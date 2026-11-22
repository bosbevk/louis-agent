namespace LouisAgent.Core.Delegation.Messages.Holes.Asks;

/// <summary>The address an ask would open too deep, and the deepest this instance allows.</summary>
public sealed record AskTooDeepHoles(string Address, int Max);
