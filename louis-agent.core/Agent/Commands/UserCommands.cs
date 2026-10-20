namespace LouisAgent.Core.Agent.Commands;

using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

/// <summary>
/// What the user types that the model never sees (S, CL15, from AgentEngine): /tools, /approve &lt;Name&gt;,
/// /reject &lt;Name&gt; and /usage, answered from the agent-built tools, the skill files and the session's usage.
/// </summary>
/// <param name="display">The currency /usage shows costs in (F14), as the engine has it at the time.</param>
internal sealed class UserCommands(AgentBuiltTools tools, SkillTools skills, Func<DisplayCurrency?> display)
{
    /// <summary>The reply, or null when the input is not one of the commands.</summary>
    /// <param name="sessionUsage">
    /// The session's usage records so far, for /usage (F3). The host keeps them: the engine serves every session of a host
    /// and doesn't know which one is asking. Null when the host doesn't track usage.
    /// </param>
    public string? Handle(string input, IReadOnlyList<UsageRecord>? sessionUsage)
    {
        string[] parts = (input ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        string command = parts[0].ToLowerInvariant();
        string argument = parts.Length > 1 ? parts[1] : "";
        return command switch
        {
            "/tools" => tools.ListAgentTools() + PendingSkillFileLines() + MessageText.Format(UserText.ToolsHint),
            "/approve" => Pending(argument, tools.ApproveTool, skills.ApproveSkillFile),
            "/reject" => Pending(argument, tools.RejectTool, skills.RejectSkillFile),
            "/usage" => sessionUsage is null ? MessageText.Format(UserText.UsageNotTracked) : UsageText.Summary(sessionUsage, display()),
            _ => null,
        };
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
            return MessageText.Format(UserText.PendingToolAndSkillFile, new { Name = name, Tool = $"{name}.tool.md", File = $"{name}.md" });
        return skillFile ? onSkillFile(name) : onTool(name);
    }

    private string PendingSkillFileLines() =>
        skills.ListPendingSkillFiles() is { Length: > 0 } lines ? "\n" + lines : "";
}
