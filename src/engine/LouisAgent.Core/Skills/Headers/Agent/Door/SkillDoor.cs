using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Headers.Agent.Arguments;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Skills.Headers.Agent.Running;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Door;

namespace LouisAgent.Core.Skills.Headers.Agent.Door;

/// <summary>
/// The one door to every skill a chat may use (F97.1): with a name alone it reads the skill, what it does, how to use
/// it, its parameters and the call it was proved with; with arguments it checks them, then runs the skill. A call that
/// doesn't fit says each problem and gives the parameters, and a name the chat can't use lists the ones it can.
/// </summary>
/// <remarks>
/// One door rather than a tool per skill (Louis's pick, 2026-11-26): Claude Code reaches every skill through one
/// <c>Skill</c> tool, and a model picks the wrong tool more often as the list grows, so the chat's tool list stays the
/// size it is however many skills there are. Reading on demand is the Agent Skills spec's progressive disclosure.
/// </remarks>
/// <seealso href="https://mikhail.io/2025/10/claude-code-skills/">Inside Claude Code's skills</seealso>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
public sealed class SkillDoor(SkillLibrary library, SkillRunner runner)
{
    /// <summary>The skill read, when no arguments are given, or run with them.</summary>
    /// <param name="name">The skill's name.</param>
    /// <param name="arguments">The call's arguments; none to read the skill instead.</param>
    /// <param name="ownDrafts">The names of the drafts the asking chat made.</param>
    public string Use(string name, JsonElement? arguments, IReadOnlySet<string> ownDrafts)
    {
        Skill? skill;
        try
        {
            skill = library.Find(name, ownDrafts);
        }
        catch (SkillRefusedException refused)
        {
            return refused.Message;
        }

        if (skill is null) return Unknown(name, ownDrafts);
        if (arguments is not { ValueKind: not JsonValueKind.Undefined and not JsonValueKind.Null } given)
            return Read(skill);

        if (SkillArguments.Problems(skill.Parameters, given) is { Count: > 0 } problems)
        {
            var misfit = new ArgumentsDoNotFitHoles(skill.Name, string.Join(" ", problems),
                skill.Parameters.Schema.GetRawText());
            return MessageText.Format(SkillToolMessage.ArgumentsDoNotFit, misfit);
        }

        return runner.Run(skill, given).Text;
    }

    /// <summary>The skill as the model reads it.</summary>
    private static string Read(Skill skill)
    {
        string example = Path.Combine(skill.Folder, SkillFiles.ExampleFile);
        var read = new SkillReadHoles(skill.Name, skill.Description, skill.Instructions.Trim(),
            skill.Parameters.Schema.GetRawText(),
            File.Exists(example)
                ? File.ReadAllText(example).Trim()
                : MessageText.Format(SkillToolMessage.NoExampleKept));
        return MessageText.Format(SkillToolMessage.SkillRead, read);
    }

    /// <summary>A name the chat can't use, with the ones it can: the approved skills, then its own drafts.</summary>
    private string Unknown(string name, IReadOnlySet<string> ownDrafts)
    {
        var mayUse = library.Approved.Select(skill => skill.Name).Concat(ownDrafts.Order(StringComparer.Ordinal));
        var unknown = new NoSuchSkillForThisChatHoles(name, string.Join(", ", mayUse));
        return MessageText.Format(SkillToolMessage.NoSuchSkillForThisChat, unknown);
    }
}
