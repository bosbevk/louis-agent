namespace LouisAgent.Core.Agent.Messages.Holes.Summaries;

/// <summary>A sub-session's last prompt, the next one expected and its budget, in tokens, written out.</summary>
public sealed record SubSessionPromptBudgetReachedHoles(string Last, string Next, string Budget);
