namespace LouisAgent.Core.Guardrails.Auth;

/// <summary>What a host does at start-up with its auth mode and key (FX18).</summary>
public enum HostAuthDecision
{
    /// <summary>A key is set: start, and check it on every request.</summary>
    Start,

    /// <summary>No key, and AGENT_AUTH=off said so on purpose: start, with a warning.</summary>
    StartOpen,

    /// <summary>No key, and nothing said it may be open: don't start.</summary>
    Refuse,
}
