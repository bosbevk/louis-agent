using LouisAgent.Core.Agent;

namespace LouisAgent.Core.Hosting.Discovery;

/// <summary>
/// Finds tools somewhere outside the engine and registers them on it once the host is built (CL57). Rider's
/// MCP server is the one we have; a host hands its discovery in through <see cref="HostServices.Discovery"/>, and
/// core itself knows no IDE.
/// </summary>
/// <remarks>
/// Strategy with a Null Object (Gang of Four; Fowler's Separated Interface): the engine keeps the one thing
/// discovery needs from it, <see cref="AgentEngine.RegisterDiscoveredTool"/>, and the seam keeps the rest out, so
/// a host that runs nowhere near Rider starts no discovery and ships no client for it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public interface IToolDiscovery
{
    /// <summary>Starts looking, in the background if it talks to something, and registers what it finds on the engine.</summary>
    void Start(AgentEngine engine);
}
