using System.ComponentModel;
using LouisAgent.Core.Agent;
using ModelContextProtocol.Server;

namespace LouisAgent.Mcp.Tools;

/// <summary>
/// The agent's guidance, read-only, so a client's model can use the tools the way the agent does. The skills the
/// agent keeps reach a client through the engine's own <c>UseSkill</c>, which the host publishes with the engine's
/// other tools.
/// </summary>
/// <remarks>
/// MCP's own way for skills, SEP-2640's <c>skill://</c> resources with <c>skills/list</c>, waits for an SDK that
/// has it (F178); until then <c>UseSkill</c> lists the approved skills and reads one by name, so a list tool here
/// would only repeat it.
/// </remarks>
/// <param name="engine">The engine the server already built.</param>
[McpServerToolType]
public class SkillsToolProvider(AgentEngine engine)
{
    /// <summary>
    /// Hands over all the guidance the agent itself reads, so the client's model can use the tools the same way.
    /// </summary>
    [McpServerTool]
    [Description("Get the agent's full guidance: default instructions plus the .NET, Python, PowerShell, Bash, "
                 + "self-extension and skill documentation")]
    public string GetSkillDocumentation() => engine.SkillDocumentation;
}
