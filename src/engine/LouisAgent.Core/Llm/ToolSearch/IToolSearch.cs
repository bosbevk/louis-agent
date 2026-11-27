using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.ToolSearch;

/// <summary>
/// How one provider lets a chat send its tools without the model seeing them all (F97.3): the tools that wait are
/// still sent, marked its own way, and what the engine's <c>FindTool</c> finds is turned into the provider's way of
/// loading a tool. A provider with no such thing has <see cref="NoToolSearch"/>, and every tool goes as before.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), the shape ADR 0003 names, beside the prompt cache's and the stale thinking's: one
/// interface with an implementation per provider that differs, picked by <c>ILlmProvider.CreateToolSearch</c>. Louis's
/// pick (F97.3): our own search tool through a strategy, over Anthropic's server-side search, whose blocks the SDK's
/// adapter drops from the next request, and over a door tool for every provider.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public interface IToolSearch
{
    /// <summary>Whether this provider can load a tool on demand; when not, search is never on.</summary>
    bool Available { get; }

    /// <summary>
    /// The client that sends the chat's requests marked for this provider, below the replay so the history and the
    /// recordings stay the provider's to shape.
    /// </summary>
    /// <param name="inner">The provider's client.</param>
    /// <param name="deferred">
    /// The names of the tools that wait to be found, decided once the engine's tools are in.
    /// </param>
    IChatClient Around(IChatClient inner, Func<IReadOnlySet<string>> deferred);
}
