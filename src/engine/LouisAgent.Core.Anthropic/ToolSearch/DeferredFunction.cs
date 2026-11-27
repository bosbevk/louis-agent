using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.ToolSearch;

/// <summary>
/// A tool that waits to be found (F97.3): the same tool, with the key the SDK's adapter reads to send it with
/// <c>defer_loading</c>, so the model doesn't see it until a search loads it.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four), on Microsoft.Extensions.AI's own <see cref="DelegatingAIFunction"/>, as the replay's
/// recorded tool is: the tool's name, schema and method pass through, and only what the adapter reads is added.
/// </remarks>
internal sealed class DeferredFunction(AIFunction inner) : DelegatingAIFunction(inner)
{
    /// <summary>
    /// The key the SDK's adapter reads a tool's <c>defer_loading</c> from (its <c>Tool.DeferLoading</c>).
    /// </summary>
    private const string DeferLoading = "DeferLoading";

    /// <summary>The tool's own properties, and the one that defers it.</summary>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; } =
        new Dictionary<string, object?>(inner.AdditionalProperties) { [DeferLoading] = true };
}
