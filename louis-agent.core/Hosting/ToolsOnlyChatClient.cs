namespace LouisAgent.Core.Hosting;

using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;

/// <summary>Stand-in for hosts that never prompt a model themselves (the MCP server, <see cref="AgentHost.BuildToolHost()"/>).</summary>
internal sealed class ToolsOnlyChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(MessageText.Format(HostingErrorMessage.ToolHostHasNoModel));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(MessageText.Format(HostingErrorMessage.ToolHostHasNoModel));

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
