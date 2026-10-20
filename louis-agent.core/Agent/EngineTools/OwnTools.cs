namespace LouisAgent.Core.Agent.EngineTools;

using System.ComponentModel;
using System.Reflection;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

/// <summary>
/// The tools an engine registers itself (S, CL15, from AgentEngine): <c>ExecuteSkill</c> and the clock in every
/// session, the toolsets' objects it was given, and, as the <c>skills</c> toolset (F5), the four that let the agent
/// extend itself. The public names are the engine's, so the model sees the same tool list as before the split.
/// </summary>
internal static class OwnTools
{
    /// <param name="toolsets">A session's tool objects; null means an engine with every self-extension tool, whatever <paramref name="selfExtension"/> says.</param>
    public static void Register(ToolRegistry registry, SkillTools skillTools, AgentBuiltTools agentBuiltTools, AgentOptions options,
        IEnumerable<object>? toolsets, bool selfExtension)
    {
        registry.Add(Function(skillTools.ExecuteSkill, nameof(AgentEngine.ExecuteSkill)));
        // Today's date and time (F22): every session, whatever its toolsets, since a model doesn't know what day it is.
        registry.AddPublicMethods(new ClockTools(hostTimeZone: options.HostTimeZone));
        if (toolsets is not null)
        {
            foreach (object toolset in toolsets) registry.AddPublicMethods(toolset);
            if (selfExtension) AddSelfExtensionTools(registry, skillTools, agentBuiltTools);
            return;
        }

        AddSelfExtensionTools(registry, skillTools, agentBuiltTools);
    }

    /// <summary>The tools that let the agent extend itself: skills and agent-built tools (toolset "skills", F5).</summary>
    private static void AddSelfExtensionTools(ToolRegistry registry, SkillTools skillTools, AgentBuiltTools agentBuiltTools)
    {
        registry.Add(Function(skillTools.CreateSkillFile, nameof(AgentEngine.CreateSkillFile)));
        registry.Add(Function(skillTools.ReloadSkills, nameof(AgentEngine.ReloadSkills)));
        registry.Add(Function(agentBuiltTools.CreateTool, nameof(AgentEngine.CreateTool)));
        registry.Add(Function(agentBuiltTools.ListAgentTools, nameof(AgentEngine.ListAgentTools)));
    }

    /// <summary>One of the engine's own methods as a tool, its [Description] with LF line endings (see <see cref="AgentEngine.Normalise"/>).</summary>
    private static AIFunction Function(Delegate method, string name) =>
        AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = method.Method.GetCustomAttribute<DescriptionAttribute>() is { } description ? AgentEngine.Normalise(description.Description) : null,
        });
}
