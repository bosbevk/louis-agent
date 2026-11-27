using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Frozen;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Tools;

/// <summary>
/// A tool as a conversation was first sent it: its name, description and schema stay as they were, whatever happens to
/// the tool later, and a call to it once the engine no longer offers it, after a /reject, is answered, never run.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four), on Microsoft.Extensions.AI's <see cref="DelegatingAIFunction"/>, as
/// <c>RecordedTool</c> is. Chosen over replacing an approved tool's object, which would keep its description but let
/// a rejected script run until the chat ends, and over leaving both as they were. The model checks its earlier thinking
/// against the tools it was sent, so what it reads of a tool never changes in an open chat; and a tool the user
/// rejected must not run, open chat or not.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
internal sealed class FrozenTool(AIFunction inner, Func<string, bool> stillOffered) : DelegatingAIFunction(inner)
{
    /// <summary>What the tool said it does when the chat first sent it.</summary>
    private readonly string _description = inner.Description;

    /// <summary>The tool's parameters as the chat first sent them.</summary>
    private readonly JsonElement _schema = inner.JsonSchema.Clone();

    /// <summary>The description as first sent.</summary>
    public override string Description => _description;

    /// <summary>The schema as first sent.</summary>
    public override JsonElement JsonSchema => _schema;

    /// <summary>Runs the tool while the engine still offers it; otherwise tells the model it wasn't run.</summary>
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        if (stillOffered(Name)) return base.InvokeCoreAsync(arguments, cancellationToken);

        var gone = new ToolNoLongerOfferedHoles(Name);
        return ValueTask.FromResult<object?>(MessageText.Format(AgentToolMessage.ToolNoLongerOffered, gone));
    }
}
