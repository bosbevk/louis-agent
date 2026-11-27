using LouisAgent.Core.Llm.ToolSearch;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.ToolSearch;

/// <summary>
/// Anthropic's tool search, with our own search tool (F97.3): every tool that waits goes with <c>defer_loading</c>,
/// so the model doesn't see it until it is found, and what <c>FindTool</c> found goes back as Anthropic's tool
/// references, which the API expands into those tools' definitions.
/// </summary>
public sealed class AnthropicToolSearch : IToolSearch
{
    /// <inheritdoc />
    public bool Available => true;

    /// <inheritdoc />
    public IChatClient Around(IChatClient inner, Func<IReadOnlySet<string>> deferred) =>
        new ToolSearchChatClient(inner, deferred);
}
