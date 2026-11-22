namespace LouisAgent.Core.Delegation.Messages.Holes.Asks;

/// <summary>An asked agent's answer, who gave it, and what this reply did.</summary>
public sealed record AgentAnswerHoles(string Answer, string Address, string Did);
