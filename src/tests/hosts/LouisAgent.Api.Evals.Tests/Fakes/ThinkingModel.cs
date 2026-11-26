using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Evals.Tests.Fakes;

/// <summary>
/// A model that is still thinking when its host stops: it never answers, and gives up only when the call is
/// cancelled, as a real model's call is when the host shuts down.
/// </summary>
/// <remarks>
/// The shared <c>FakeChatClient</c> answers from a script that can't see the call's cancellation, so a held answer
/// would keep a stopping host waiting for its shutdown timeout. This one ends with the stop.
/// </remarks>
internal sealed class ThinkingModel : IChatClient
{
    private readonly TaskCompletionSource _asked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Done once the run has reached the model, so a test knows the run is going before it cuts it.</summary>
    public Task Asked => _asked.Task;

    /// <summary>Thinks until the call is cancelled.</summary>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _asked.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>Thinks until the call is cancelled, as the answer that never comes.</summary>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var answer = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, answer.Text);
    }

    /// <summary>The model offers no services.</summary>
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <summary>Nothing to let go of.</summary>
    public void Dispose()
    {
    }
}
