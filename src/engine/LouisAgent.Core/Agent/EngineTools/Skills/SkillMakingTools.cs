using System.ComponentModel;
using System.Text.Json;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Prompting.Headers.Agent.Drafting;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The model's tool for making a skill (F97.1): <c>CreateSkill</c> drafts it, kept only once its example has run, and
/// the chat whose turn is running notes it as its own, so it can use it at once through <c>UseSkill</c> and no other
/// chat can until the person approves it.
/// </summary>
/// <remarks>
/// The chat comes from the running turn's scope (<see cref="TurnTools"/>, an Ambient Context), as the door's does. The
/// drafting is made when first used, as the engine's skills folder is set after it is built.
/// </remarks>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
internal sealed class SkillMakingTools(Func<SkillDrafting> drafting)
{
    /// <summary>Makes a skill, proved by its example, as this chat's draft.</summary>
    [Description("""
                 Makes a skill you can use at once in this chat through UseSkill, and every chat once the person
                 approves it. A skill is a folder: its SKILL.md, a JSON Schema of its parameters, and one script that
                 reads its arguments as a JSON object from stdin and prints its answer. It is kept only if the example
                 call runs cleanly; otherwise nothing is kept and the answer says what to fix.
                 """)]
    public string CreateSkill(
        [Description("The skill's name: lowercase letters, digits and hyphens, such as word-count.")]
        string name,
        [Description("""
                     The SKILL.md: YAML front matter with name (the skill's name), description (what it does, then
                     when to use it) and metadata.script (its script's path, such as scripts/count.py, a .py, .ps1
                     or .sh file), then a body saying how to use it.
                     """)]
        string skillFile,
        [Description("""
                     The parameters, a JSON Schema object: type object, each parameter under properties with its type
                     and description, and the ones a call must give under required.
                     """)]
        string parameters,
        [Description("The script, written to the path metadata.script names.")]
        string script,
        [Description("One example call's arguments, a JSON object of the parameters, run to prove the skill.")]
        JsonElement example)
    {
        var answer = drafting().Draft(name, skillFile, parameters, script, example);
        if (answer.Succeeded) TurnTools.Current?.Chat.Drafted(name);
        return answer.Text;
    }
}
