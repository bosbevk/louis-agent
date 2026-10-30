using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared;

/// <summary>The roles the Puls evals name (ADR 0043: each eval set keeps its own enum of the names it uses): the cook, the specialist a question about a meal is routed to and the role a turn that started it reports. A member's wire name is the role's name in a session's stream.</summary>
public enum PulsEvalRole
{
    [WireName("puls-cook")] Cook,
}
