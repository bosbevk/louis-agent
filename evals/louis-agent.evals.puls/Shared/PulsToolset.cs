namespace LouisAgent.Evals.Puls.Shared;

/// <summary>
/// The toolsets the Puls evals name in a mode (ADR 0043: each eval set keeps its own enum of the names it uses). A member's lower-case name is its wire name,
/// the one a run's protocol line and a session request carry.
/// </summary>
public enum PulsToolset
{
    Files,
    Git,
    Web,
    Shops,
    Specialists,
    Minions,
    Skills,
}
