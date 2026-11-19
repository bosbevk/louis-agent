using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>The sessions never prompt a model in these tests.</summary>
internal sealed class NoModel : IChatClient
{
    /// <summary>
    /// Never asked: these tests open sessions, they don't talk to a model.
    /// </summary>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Never asked either, streaming or not.
    /// </summary>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>
    /// Offers no extra services.
    /// </summary>
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <summary>
    /// Holds nothing, so there's nothing to let go of.
    /// </summary>
    public void Dispose()
    {
    }
}
