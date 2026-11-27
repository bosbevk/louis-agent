using LouisAgent.Core.Agent.EngineTools.ToolSearch;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.ToolSearch;

/// <summary>
/// Shapes each request for Anthropic's tool search (F97.3): the tools that wait go deferred, and FindTool's answers go
/// back as tool references. It works on copies, so the chat's history and options are never changed (ADR 0008).
/// </summary>
/// <param name="inner">The provider's client.</param>
/// <param name="deferred">The names of the tools that wait, the engine's decision.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
internal sealed class ToolSearchChatClient(IChatClient inner, Func<IReadOnlySet<string>> deferred)
    : DelegatingChatClient(inner)
{
    /// <summary>The engine's search tool, whose answers become tool references.</summary>
    private const string FindTool = nameof(ToolSearchTools.FindTool);

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(Referenced(messages), Marked(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(Referenced(messages), Marked(options), cancellationToken);

    /// <summary>
    /// A copy of the options whose waiting tools are deferred; the options as they are when none waits.
    /// </summary>
    private ChatOptions? Marked(ChatOptions? options)
    {
        var waiting = deferred();
        if (options?.Tools is not { Count: > 0 } tools || waiting.Count == 0) return options;

        ChatOptions copy = options.Clone();
        copy.Tools = [.. tools.Select(tool => Deferred(tool, waiting))];
        return copy;
    }

    /// <summary>A tool deferred when it waits, the tool itself when it doesn't.</summary>
    private static AITool Deferred(AITool tool, IReadOnlySet<string> waiting) =>
        tool is AIFunction function && waiting.Contains(function.Name) ? new DeferredFunction(function) : tool;

    /// <summary>
    /// The messages, with each answer of FindTool's as tool references; the same list when it was never called.
    /// </summary>
    private static IEnumerable<ChatMessage> Referenced(IEnumerable<ChatMessage> messages)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        var searches = list.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
            .Where(call => call.Name == FindTool)
            .Select(call => call.CallId)
            .ToHashSet();
        return searches.Count == 0 ? list : list.Select(message => WithReferences(message, searches)).ToList();
    }

    /// <summary>
    /// A copy of a message whose FindTool answers are tool references; the message itself when it has none.
    /// </summary>
    private static ChatMessage WithReferences(ChatMessage message, IReadOnlySet<string> searches)
    {
        if (!message.Contents.OfType<FunctionResultContent>().Any(result => searches.Contains(result.CallId)))
            return message;

        ChatMessage copy = message.Clone();
        copy.Contents = [.. message.Contents.Select(content => Referenced(content, searches))];
        return copy;
    }

    /// <summary>
    /// A FindTool answer as its references, or the content as it was: any other content, or an answer that names no
    /// tool, since a tool result with no references would say nothing.
    /// </summary>
    private static AIContent Referenced(AIContent content, IReadOnlySet<string> searches)
    {
        if (content is not FunctionResultContent result || !searches.Contains(result.CallId)) return content;
        var references = ToolReferences.Of(result.Result);
        return references.Count == 0 ? content : new FunctionResultContent(result.CallId, references);
    }
}
