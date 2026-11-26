using System.ComponentModel;
using System.Text.Json;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Skills.Headers.Agent.Door;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The one door to every skill, as a tool in every chat (F97.1): <c>UseSkill</c> reads a skill by its name, or runs it
/// with arguments, for the chat whose turn is running, so it reaches that chat's drafts and no other's.
/// </summary>
/// <remarks>
/// The chat comes from the running turn's scope (<see cref="TurnTools"/>, an Ambient Context, as the round limit's
/// final request reads its tools), since a tool call carries only its arguments. The door is made when first used, as
/// the engine's skills folder is set after it is built.
/// </remarks>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
internal sealed class SkillDoorTools(Func<SkillDoor> door)
{
    /// <summary>The names no chat drafted, for a call made outside a turn.</summary>
    private static readonly IReadOnlySet<string> NoDrafts = new HashSet<string>();

    /// <summary>Reads a skill by its name, or runs it with arguments.</summary>
    [Description("""
                 Uses one of your skills. Give its name alone to read it: what it does, how to use it, its parameters
                 and the call it was proved with. Give its name and arguments, a JSON object of its parameters, to run
                 it and get its answer. A skill you made in this chat is yours to use now, before it is approved.
                 The approved skills:
                 """)]
    public string UseSkill(
        [Description("The skill's name, as listed, or as you made it in this chat.")]
        string name,
        [Description("A JSON object of the skill's parameters, to run it; leave it out to read the skill.")]
        JsonElement? arguments = null) =>
        door().Use(name, arguments, TurnTools.Current?.Drafts ?? NoDrafts);
}
