using LouisAgent.Core.Config;
using LouisAgent.Core.Agent.EngineTools.Clock;
using LouisAgent.Core.Agent.EngineTools.Skills;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools;

/// <summary>
/// The tools an engine registers itself (S, CL15, from AgentEngine): the clock in every session, the toolsets'
/// objects it was given, and, as the <c>skills</c> toolset (F5), the ones for the skills the agent makes and keeps
/// (F97.1). The public names are the engine's, so the model sees the same tool list as before the split.
/// </summary>
internal static class OwnTools
{
    /// <summary>Registers the clock, the toolsets' tools and, where the session has them, the skills' tools.</summary>
    /// <param name="registry">Where the tools go.</param>
    /// <param name="agentSkills">The tools for the skills the agent makes and keeps (F97.1).</param>
    /// <param name="options">The engine's settings.</param>
    /// <param name="toolsets">
    /// A session's tool objects; null means an engine with every self-extension tool, whatever
    /// <paramref name="selfExtension"/> says.
    /// </param>
    /// <param name="selfExtension">Whether the session has the skills toolset.</param>
    /// <param name="clock">The clock the clock tool reads.</param>
    public static void Register(ToolRegistry registry, AgentSkillTools agentSkills, AgentOptions options,
        IEnumerable<object>? toolsets, bool selfExtension, TimeProvider? clock = null)
    {
        // Today's date and time (F22): every session, whatever its toolsets, since a model doesn't know what day it is.
        registry.AddPublicMethods(new ClockTools(clock, options.HostTimeZone));
        if (toolsets is not null)
        {
            foreach (object toolset in toolsets) registry.AddPublicMethods(toolset);
            if (selfExtension) AddSkillTools(registry, agentSkills);
            return;
        }

        AddSkillTools(registry, agentSkills);
    }

    /// <summary>
    /// The tools for the skills an agent keeps (F97.1): the door, <c>UseSkill</c>, its description followed by the
    /// approved skills; <c>CreateSkill</c>; and <c>ReloadSkills</c>.
    /// </summary>
    public static AgentSkillTools AgentSkills(SkillDoorTools door, Func<string> listing, SkillMakingTools making,
        SkillReloadTools reload) =>
        new(new ListedSkillsFunction(Function(door.UseSkill, nameof(SkillDoorTools.UseSkill)), listing),
            Function(making.CreateSkill, nameof(SkillMakingTools.CreateSkill)),
            Function(reload.ReloadSkills, nameof(SkillReloadTools.ReloadSkills)));

    /// <summary>
    /// The skills toolset's tools (F5, F97.1): only a chat whose agent can make and keep skills has them, so no other
    /// chat's tools grow.
    /// </summary>
    private static void AddSkillTools(ToolRegistry registry, AgentSkillTools agentSkills)
    {
        registry.Add(agentSkills.Door);
        registry.Add(agentSkills.Create);
        registry.Add(agentSkills.Reload);
    }

    /// <summary>
    /// One of the engine's own methods as a tool, its descriptions with LF line endings (see
    /// <see cref="ToolFunctionOptions"/>).
    /// </summary>
    private static AIFunction Function(Delegate method, string name) =>
        AIFunctionFactory.Create(method, ToolFunctionOptions.For(method.Method, name));
}
