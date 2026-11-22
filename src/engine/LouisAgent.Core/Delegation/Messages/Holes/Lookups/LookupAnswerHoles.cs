namespace LouisAgent.Core.Delegation.Messages.Holes.Lookups;

/// <summary>A lookup agent's answer, who gave it, the model it ran on and the tool it was handed.</summary>
public sealed record LookupAnswerHoles(string Answer, string Role, string Llm, string Tool);
