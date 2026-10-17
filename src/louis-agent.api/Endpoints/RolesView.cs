namespace LouisAgent.Api.Endpoints;

using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills.Roles;

/// <summary>
/// The roles a session can choose (F21), per level (CL8: a session's level is its depth); <c>default</c> is the voice a
/// level-0 session gets when it names none (CL9). Served at <c>GET /roles</c> (CL14) and, for a client one deploy behind,
/// at <c>GET /personalities</c>, the same body, until the TODO's line removes it.
/// </summary>
internal static class RolesView
{
    public static readonly IReadOnlyList<string> Routes = ["/roles", "/personalities"];

    public static object Of(AgentEngineFactory engines) => new
    {
        @default = engines.DefaultVoice,
        agent = engines.AvailableRoles(RoleLevel.Agent),
        specialists = engines.AvailableRoles(RoleLevel.Specialist),
        minions = engines.AvailableRoles(RoleLevel.Minion),
    };
}
