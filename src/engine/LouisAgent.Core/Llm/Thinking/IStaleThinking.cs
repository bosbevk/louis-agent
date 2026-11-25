using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.Thinking;

/// <summary>
/// What a provider does with a resumed chat's saved thinking once a release changed the chat's tools: the model checks
/// that thinking against everything sent before it, the tools included, so it may no longer match.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), as ADR 0003 names it: providers differ in what they can do with thinking that no longer
/// matches, so each says its own way through its <c>ILlmProvider</c>, and nothing else branches on a provider's name.
/// The default, <see cref="StripStaleThinking"/>, works for every provider.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
public interface IStaleThinking
{
    /// <summary>The saved turns' messages, as a chat resumed on moved tools sends them.</summary>
    IReadOnlyList<ChatMessage> Restore(IReadOnlyList<ChatMessage> saved);

    /// <summary>
    /// A request of such a chat, as this provider sends it; unchanged unless the provider needs to ask for something
    /// on every request, as Anthropic asks to drop the blocks that fail.
    /// </summary>
    ChatOptions Prepare(ChatOptions options) => options;
}
