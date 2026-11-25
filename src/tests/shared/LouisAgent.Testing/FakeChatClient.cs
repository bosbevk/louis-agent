using Microsoft.Extensions.AI;

namespace LouisAgent.Testing;

/// <summary>Scripted IChatClient: each call is answered by the next delegate and the requests are recorded.</summary>
public sealed class FakeChatClient(params Func<IList<ChatMessage>, ChatResponse>[] script) : IChatClient
{
    /// <summary>Which answer of the script comes next.</summary>
    private int _call;

    /// <summary>Every request, in order: a copy of its messages, and its options as they were passed.</summary>
    public List<(List<ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = [];

    /// <summary>
    /// The names of the tools each request carried, read when it was sent: <see cref="Requests"/> keeps the options
    /// themselves, whose tool list may change after.
    /// </summary>
    public List<IReadOnlyList<string>> ToolNamesSent { get; } = [];

    /// <summary>Each request's tools as the model read them when it was sent: each one's description, by name.</summary>
    public List<IReadOnlyDictionary<string, string>> ToolDescriptionsSent { get; } = [];

    /// <summary>Records the request and answers with the next scripted reply.</summary>
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var snapshot = messages.ToList();
        Requests.Add((snapshot, options));
        ToolNamesSent.Add([.. options?.Tools?.Select(tool => tool.Name) ?? []]);
        ToolDescriptionsSent.Add((options?.Tools ?? []).ToDictionary(tool => tool.Name, tool => tool.Description));
        return Task.FromResult(script[_call++](snapshot));
    }

    /// <summary>
    /// Streams the scripted response as one update per content item, ending with its finish reason (default Stop, as
    /// real providers send).
    /// </summary>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var message in response.Messages)
            foreach (var content in message.Contents)
                yield return new ChatResponseUpdate(message.Role, [content]) { MessageId = message.MessageId };
        yield return new ChatResponseUpdate { FinishReason = response.FinishReason ?? ChatFinishReason.Stop };
    }

    /// <summary>The fake offers no services.</summary>
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <summary>Nothing to let go of.</summary>
    public void Dispose()
    {
    }
}
