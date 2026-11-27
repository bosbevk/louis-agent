using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.ToolSearch;

/// <summary>
/// A provider that can't load a tool on demand (F97.3): search is never on, and the client is the provider's own, so
/// every tool goes to the model as before.
/// </summary>
/// <remarks>Null Object, the default of <c>ILlmProvider.CreateToolSearch</c>, as <c>NoPromptCaching</c> is.</remarks>
public sealed class NoToolSearch : IToolSearch
{
    /// <summary>The one instance; it holds nothing.</summary>
    public static NoToolSearch Instance { get; } = new();

    private NoToolSearch()
    {
    }

    /// <inheritdoc />
    public bool Available => false;

    /// <inheritdoc />
    public IChatClient Around(IChatClient inner, Func<IReadOnlySet<string>> deferred) => inner;
}
