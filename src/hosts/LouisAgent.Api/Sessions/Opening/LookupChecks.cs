using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Lookups;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Api.Sessions.Opening;

/// <summary>
/// A lookup agent's checks (F30), in code and before any HTTP or engine: capability delegation means it can never do
/// more than the subagent that sent it, which hands it one tool of its own (Louis: no list per role; the subagent's
/// toolbox is the only one). It names its tool, it names its parent, the parent runs here, the tool is one that may be
/// handed down, and the parent has it. The role itself is the factory's check, as for every role.
/// </summary>
/// <remarks>
/// Split out of <see cref="SessionOpening"/> (CL70) so each file holds one concern under ADR 0010's size; guard
/// clauses (Fowler), one refusal each, in the order a caller would fix them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0021-minions-one-task-one-tool.md">ADR 0021: one task, one tool</seealso>
internal sealed class LookupChecks(
    UsageTags? tags,
    string? tool,
    string? role,
    Func<string, AgentSession?> sessionOf)
{
    /// <summary>Runs every check; the first that fails throws its <see cref="ToolsetException"/>.</summary>
    public void Run()
    {
        if (tool is null)
        {
            var noTool = new LookupNeedsToolHoles(role ?? "(none)");
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.LookupNeedsTool, noTool));
        }

        if (string.IsNullOrWhiteSpace(tags?.Parent))
        {
            var noParent = new LookupNeedsParentHoles("sess_…#3");
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.LookupNeedsParent, noParent));
        }

        string parentId = tags.Parent.Split('#')[0];
        if (sessionOf(parentId) is not { } parent)
        {
            var unknownParent = new LookupParentUnknownHoles(parentId);
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.LookupParentUnknown, unknownParent));
        }

        if (LookupAsk.NeverHandedDown.Contains(tool))
        {
            var neverHanded = new ToolNeverHandedDownHoles(tool);
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.ToolNeverHandedDown, neverHanded));
        }

        if (parent.Engine.Tools.Any(t => string.Equals(t.Name, tool, StringComparison.Ordinal))) return;
        var lacks = new ParentLacksToolHoles(tool);
        throw new ToolsetException(MessageText.Format(ApiErrorMessage.ParentLacksTool, lacks));
    }
}
