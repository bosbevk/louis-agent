using System.Reflection;
using LouisAgent.Mcp.Tools;
using ModelContextProtocol.Server;

namespace LouisAgent.Mcp.Tests.Tools;

/// <summary>
/// F97.1 step 14d: the MCP host's own skill tool is the agent's guidance alone. The skills the agent keeps reach an
/// MCP client through the engine's <c>UseSkill</c>, which the host publishes with the engine's other tools.
/// </summary>
public class SkillsToolProviderTests
{
    /// <summary>Its one MCP tool is the guidance; the old skill sections' list and info are gone.</summary>
    [Test]
    public void ItsOneTool_IsTheGuidance()
    {
        var tools = typeof(SkillsToolProvider)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(method => method.Name);

        Assert.That(tools, Is.EqualTo(new[] { nameof(SkillsToolProvider.GetSkillDocumentation) }));
    }
}
