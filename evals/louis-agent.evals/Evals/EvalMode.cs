namespace LouisAgent.Evals.Evals;

/// <summary>One mode a task runs in: the role it talks to, the toolsets it opens with, and (for a sub-session) its depth and LLM.</summary>
public sealed record EvalMode(string Name, string Role, IReadOnlyList<string> Toolsets, int Depth = 0, string? Llm = null);
