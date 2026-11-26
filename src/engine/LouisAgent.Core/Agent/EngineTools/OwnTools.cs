
using System.ComponentModel;
using System.Reflection;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent.EngineTools.Clock;
using LouisAgent.Core.Agent.EngineTools.Skills;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools;
/// <summary>
/// The tools an engine registers itself (S, CL15, from AgentEngine): <c>ExecuteSkill</c> and the clock in every
/// session, the toolsets' objects it was given, and, as the <c>skills</c> toolset (F5), the ones that let the agent
/// extend itself, with the door to the skills it keeps (F97.1). The public names are the engine's, so the model sees
/// the same tool list as before the split.
/// </summary>
internal static class OwnTools
{
    /// <param name="registry">Where the tools go.</param>
    /// <param name="skillTools">The skill tools.</param>
    /// <param name="agentBuiltTools">The agent-built tools.</param>
    /// <param name="door">The one door to every skill, <c>UseSkill</c>, listing the approved skills (F97.1).</param>
    /// <param name="options">The engine's settings.</param>
    /// <param name="toolsets">
    /// A session's tool objects; null means an engine with every self-extension tool, whatever
    /// <paramref name="selfExtension"/> says.
    /// </param>
    /// <param name="selfExtension">Whether the session has the skills toolset.</param>
    /// <param name="clock">The clock the clock tool reads.</param>
    public static void Register(ToolRegistry registry, SkillTools skillTools, AgentBuiltTools agentBuiltTools,
        AIFunction door, AgentOptions options, IEnumerable<object>? toolsets, bool selfExtension,
        TimeProvider? clock = null)
    {
        registry.Add(Function(skillTools.ExecuteSkill, nameof(AgentEngine.ExecuteSkill)));
        // Today's date and time (F22): every session, whatever its toolsets, since a model doesn't know what day it is.
        registry.AddPublicMethods(new ClockTools(clock, options.HostTimeZone));
        if (toolsets is not null)
        {
            foreach (object toolset in toolsets) registry.AddPublicMethods(toolset);
            if (selfExtension) AddSelfExtensionTools(registry, skillTools, agentBuiltTools, door);
            return;
        }

        AddSelfExtensionTools(registry, skillTools, agentBuiltTools, door);
    }

    /// <summary>
    /// The one door to every skill, <c>UseSkill</c> (F97.1), its description followed by the approved skills, one line
    /// each.
    /// </summary>
    public static AIFunction Door(SkillDoorTools door, Func<string> listing) =>
        new ListedSkillsFunction(Function(door.UseSkill, nameof(SkillDoorTools.UseSkill)), listing);

    /// <summary>
    /// The tools that let the agent extend itself (toolset "skills", F5), and the door to the skills it keeps
    /// (F97.1): only a chat whose agent can make and keep skills has them, so no other chat's tools grow.
    /// </summary>
    private static void AddSelfExtensionTools(ToolRegistry registry, SkillTools skillTools,
        AgentBuiltTools agentBuiltTools, AIFunction door)
    {
        registry.Add(door);
        registry.Add(Function(skillTools.CreateSkillFile, nameof(AgentEngine.CreateSkillFile)));
        registry.Add(Function(skillTools.ReloadSkills, nameof(AgentEngine.ReloadSkills)));
        registry.Add(Function(agentBuiltTools.CreateTool, nameof(AgentEngine.CreateTool)));
        registry.Add(Function(agentBuiltTools.ListAgentTools, nameof(AgentEngine.ListAgentTools)));
    }

    /// <summary>
    /// One of the engine's own methods as a tool, its [Description] with LF line endings (see
    /// <see cref="AgentEngine.Normalise"/>).
    /// </summary>
    private static AIFunction Function(Delegate method, string name) =>
        AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = method.Method.GetCustomAttribute<DescriptionAttribute>() is { } description
                ? AgentEngine.Normalise(description.Description)
                : null,
        });
}
