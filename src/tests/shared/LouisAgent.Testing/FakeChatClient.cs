using Microsoft.Extensions.AI;

namespace LouisAgent.Testing;

/// <summary>Scripted IChatClient: each call is answered by the next delegate and the requests are recorded.</summary>
public sealed class FakeChatClient(params Func<IList<ChatMessage>, ChatResponse>[] script) : IChatClient
{
    private int _call;
    public List<(List<ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var snapshot = messages.ToList();
        Requests.Add((snapshot, options));
        return Task.FromResult(script[_call++](snapshot));
    }

    /// <summary>Streams the scripted response as one update per content item, ending with its finish reason (default Stop, as real providers send).</summary>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var message in response.Messages)
            foreach (var content in message.Contents)
                yield return new ChatResponseUpdate(message.Role, [content]) { MessageId = message.MessageId };
        yield return new ChatResponseUpdate { FinishReason = response.FinishReason ?? ChatFinishReason.Stop };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
