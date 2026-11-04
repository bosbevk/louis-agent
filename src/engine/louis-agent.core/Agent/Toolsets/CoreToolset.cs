namespace LouisAgent.Core.Agent.Toolsets;

/// <summary>
/// The toolsets core itself offers (F5), as the names they travel under (ADR 0043: a name that repeats is an enum). A member's lower-case name is
/// its wire name, the one in a request body, a role file and <c>GET /v1/toolsets</c>. Core's own code, the hosts and their tests share this one;
/// a pack's toolset (<c>shops</c>) is the pack's, in the pack's own enum. The catalog stays open: a pack adds names core does not list.
/// </summary>
public enum CoreToolset
{
    Files,
    Git,
    DotNet,
    Python,
    PowerShell,
    Bash,
    Web,
    Paymo,
    DevOps,
    Specialists,
    Minions,
    Skills,
}
