using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Messages.Holes.Drafts;
using LouisAgent.Core.Messages.Holes.SkillFiles;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Drafting;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Door;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Core.Agent.Commands;

/// <summary>
/// What the user types that the model never sees (S, CL15, from AgentEngine): /skills, /approve &lt;Name&gt;,
/// /reject &lt;Name&gt; and /usage, answered from the skills the agent made, the skill files and the session's usage.
/// </summary>
/// <param name="skills">The skill files.</param>
/// <param name="library">The skills the agent made, drafts and approved (F97.1), read when first needed.</param>
/// <param name="display">The currency /usage shows costs in (F14), as the engine has it at the time.</param>
internal sealed class UserCommands(
    SkillTools skills,
    Func<SkillLibrary> library,
    Func<DisplayCurrency?> display)
{
    /// <summary>The reply, or null when the input is not one of the commands.</summary>
    /// <param name="input">What the user typed.</param>
    /// <param name="sessionUsage">
    /// The session's usage records so far, for /usage (F3). The host keeps them: the engine serves every session of a
    /// host and doesn't know which one is asking. Null when the host doesn't track usage.
    /// </param>
    /// <param name="chat">
    /// The chat the command was typed in, whose model is told on its next turn what /approve or /reject did (F97.1);
    /// none when the host doesn't say.
    /// </param>
    public string? Handle(string input, IReadOnlyList<UsageRecord>? sessionUsage, Conversation? chat = null)
    {
        string[] parts = (input ?? "").Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        string command = parts[0].ToLowerInvariant();
        string argument = parts.Length > 1 ? parts[1] : "";
        return command switch
        {
            "/skills" => Skills(),
            "/approve" => Decided(library().Approve(argument), argument, SkillToolMessage.DraftApprovedNote, chat)
                          ?? skills.ApproveSkillFile(argument),
            "/reject" => Decided(library().Reject(argument), argument, SkillToolMessage.DraftRejectedNote, chat)
                         ?? skills.RejectSkillFile(argument),
            "/usage" => sessionUsage is null
                ? MessageText.Format(UserText.UsageNotTracked)
                : UsageText.Summary(sessionUsage, display()),
            _ => null,
        };
    }

    /// <summary>
    /// The drafts waiting for /approve or /reject, each with the call it was proved with (F97.1), then the approved
    /// skills.
    /// </summary>
    private string Skills()
    {
        var drafts = library().Drafts;
        var lines = drafts.Count == 0
            ? [MessageText.Format(UserText.NoDraftsWaiting)]
            : drafts.Select(Listed).Prepend(MessageText.Format(UserText.DraftsHeading));
        return string.Join('\n', lines.Append(ApprovedLine(library().Approved)));
    }

    /// <summary>The approved skills' names on one line, or a line saying there are none.</summary>
    private static string ApprovedLine(IReadOnlyList<Skill> approved)
    {
        if (approved.Count == 0) return MessageText.Format(UserText.NoApprovedSkills);

        var names = new ApprovedSkillsListedHoles(string.Join(", ", approved.Select(s => s.Name)));
        return MessageText.Format(UserText.ApprovedSkillsListed, names);
    }

    /// <summary>
    /// The person's reply to a draft approved or rejected, with a note kept for the chat's model, which never sees the
    /// command; none when there was no draft by that name.
    /// </summary>
    private static string? Decided(string? reply, string name, SkillToolMessage note, Conversation? chat)
    {
        if (reply is null) return null;

        var decided = new DraftDecidedHoles(name);
        chat?.Note(MessageText.Format(note, decided));
        return reply;
    }

    /// <summary>A draft's line: its name, what it does, and its example.</summary>
    private static string Listed(Skill draft)
    {
        var listed = new DraftListedHoles(draft.Name, draft.Description, SkillExample.TextOf(draft) ?? "");
        return MessageText.Format(UserText.DraftListed, listed);
    }
}
