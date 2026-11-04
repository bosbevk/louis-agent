using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Puls.Text;

/// <summary>
/// How the work panel marks one step of a specialist or minion: still waiting, failed, or done, as a class name for
/// its colour and as the sign beside it.
/// </summary>
/// <remarks>
/// A switch on the step's shape, one arm per case read like a table, instead of a ternary inside a ternary in the
/// markup; kept out of the component so it has its own tests.
/// </remarks>
public static class StepMarks
{
    /// <summary>The step's state, as the class name its row gets.</summary>
    public static string StateOf(Block step) => step switch
    {
        { ToolResult: null } => "waiting",
        { ToolFailed: true } => "failed",
        _ => "ok",
    };

    /// <summary>The sign beside the step: dots while it waits, a cross when it failed, a tick when it's done.</summary>
    public static string MarkOf(Block step) => step switch
    {
        { ToolResult: null } => "⋯",
        { ToolFailed: true } => "✗",
        _ => "✓",
    };
}
