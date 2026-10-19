namespace LouisAgent.Web.Shared.Chat;

using LouisAgent.Web.Shared.Text;

/// <summary>The line under a running turn (F34): what its specialist is doing, by its role's phases, else just busy.</summary>
public static class WorkStatus
{
    /// <summary>
    /// A specialist still at work whose role has phases: <see cref="RolePhases.Building"/> once it has a tool's result and
    /// is thinking or writing after it, else <see cref="RolePhases.Gathering"/> (starting, or searching again). Anything
    /// else is <see cref="ChatText.Processing"/>.
    /// </summary>
    public static string Label(Turn turn, ChatText text)
    {
        if (turn.Blocks.LastOrDefault() is not { Kind: BlockKind.Tool, ToolResult: null } call
            || Delegation.From(call) is not { Role: { } role }
            || !text.RolePhases.TryGetValue(role, out var phases))
            return text.Processing;

        var work = call.SubTurn?.Blocks ?? [];
        bool building = work.LastOrDefault() is { Kind: BlockKind.Thinking or BlockKind.Text }
            && work.Any(b => b is { Kind: BlockKind.Tool, ToolResult: not null });
        return building ? phases.Building : phases.Gathering;
    }
}
