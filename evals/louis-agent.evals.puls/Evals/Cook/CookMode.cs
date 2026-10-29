using LouisAgent.Choices;

namespace LouisAgent.Evals.Puls.Evals.Cook;

/// <summary>
/// The modes a Puls eval runs in (ADR 0043: each eval set keeps its own enum of the names it uses): Puls with its own shop tools
/// (<c>puls-tools</c>) and Puls with its cook (<c>puls-cook</c>). A mode is a run's label for one way of running the same conversations, not a Puls mode file and not a role.
/// </summary>
public enum CookMode
{
    [WireName("puls-tools")] Tools,
    [WireName("puls-cook")] Cook,
}
