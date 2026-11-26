using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Messages.Holes.Drafts;
using LouisAgent.Core.Messages.Holes.SkillFiles;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Drafting;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Core.Agent.Commands;

/// <summary>
/// What the user types that the model never sees (S, CL15, from AgentEngine): /skills, /tools, /approve &lt;Name&gt;,
/// /reject &lt;Name&gt; and /usage, answered from the skills the agent made, the agent-built tools, the skill files
/// and the session's usage.
/// </summary>
/// <param name="tools">The agent-built tools.</param>
/// <param name="skills">The skill files.</param>
/// <param name="library">The skills the agent made, drafts and approved (F97.1), read when first needed.</param>
/// <param name="display">The currency /usage shows costs in (F14), as the engine has it at the time.</param>
internal sealed class UserCommands(
    AgentBuiltTools tools,
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
    public string? Handle(string input, IReadOnlyList<UsageRecord>? sessionUsage)
    {
        string[] parts = (input ?? "").Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        string command = parts[0].ToLowerInvariant();
        string argument = parts.Length > 1 ? parts[1] : "";
        return command switch
        {
            "/skills" => Skills(),
            "/tools" => tools.ListAgentTools() + PendingSkillFileLines() + MessageText.Format(UserText.ToolsHint),
            "/approve" => library().Approve(argument) ?? Pending(argument, tools.ApproveTool, skills.ApproveSkillFile),
            "/reject" => library().Reject(argument) ?? Pending(argument, tools.RejectTool, skills.RejectSkillFile),
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
        var approved = new ApprovedSkillsListedHoles(string.Join(", ", library().Approved.Select(s => s.Name)));
        return string.Join('\n', lines.Append(MessageText.Format(UserText.ApprovedSkillsListed, approved)));
    }

    /// <summary>A draft's line: its name, what it does, and its example.</summary>
    private static string Listed(Skill draft)
    {
        var listed = new DraftListedHoles(draft.Name, draft.Description, SkillExample.TextOf(draft) ?? "");
        return MessageText.Format(UserText.DraftListed, listed);
    }

    /// <summary>
    /// /approve and /reject reach an agent-built tool or a skill file the agent wrote (FX20), by name: a tool when one
    /// is pending, else the skill file, else the tool's own "nothing pending" reply; a name that is both is refused.
    /// </summary>
    private string Pending(string name, Func<string, string> onTool, Func<string, string> onSkillFile)
    {
        bool tool = !string.IsNullOrWhiteSpace(name) && tools.HasPendingTool(name);
        bool skillFile = !string.IsNullOrWhiteSpace(name) && skills.HasPendingSkillFile(name);
        if (tool && skillFile)
        {
            var both = new PendingToolAndSkillFileHoles(name, $"{name}.tool.md", $"{name}.md");
            return MessageText.Format(UserText.PendingToolAndSkillFile, both);
        }

        return skillFile ? onSkillFile(name) : onTool(name);
    }

    /// <summary>The skill files waiting, after the tools /tools lists; nothing when none wait.</summary>
    private string PendingSkillFileLines() =>
        skills.ListPendingSkillFiles() is { Length: > 0 } lines ? "\n" + lines : "";
}
