namespace LouisAgent.Orchestrator.Client;

/// <summary>The machine-readable line louis-agent is asked to end a fix with.</summary>
internal sealed record FixResult(
    string Status,
    string? Commit,
    string? Branch,
    string? Tests,
    string? Summary,
    string? Reason);
