using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using LouisAgent.Composition.Logging;
using LouisAgent.Core.Llm.Messages;
using LouisAgent.Core.Llm.Messages.Holes.Failures;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Llm.Failures;

/// <summary>
/// Asks the model again after a busy moment, but only before it has said anything (F8). A request that fails in a way
/// its provider calls worth asking again is tried again after a short wait, up to <paramref name="maxRetries"/> more
/// times; once any content has gone out it never is, so nothing is said twice. An account limit is never tried again:
/// it becomes an <see cref="AccountLimitException"/> with the provider's reason. Anything else is passed on as it was.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four), on Microsoft.Extensions.AI's own <see cref="DelegatingChatClient"/>, the shape its docs
/// give for a client that adds behaviour around another. Louis's pick (F8): our own client, over upgrading to MEAI 11's
/// experimental <c>FailoverChatClient</c> and over leaving it to the SDK, which retries what fails before a stream but
/// not inside one. A stream's opening event carries no content, so updates without any are held back until the first
/// one with content: a retry drops them, and the caller sees the opening once.
/// </remarks>
/// <param name="inner">The provider's client.</param>
/// <param name="failures">How the provider names its failures.</param>
/// <param name="maxRetries">How many more times a busy moment is asked again; 0 asks once.</param>
/// <param name="wait">How long to wait before try <c>n</c> (from 1); a test passes none.</param>
public sealed class RetryingChatClient(
    IChatClient inner,
    IModelFailures failures,
    int maxRetries,
    Func<int, CancellationToken, Task>? wait = null) : DelegatingChatClient(inner)
{
    /// <summary>How many more times a busy moment is asked again by default: two, as the SDK's own retries.</summary>
    public const int DefaultRetries = 2;

    /// <summary>The first wait, doubled for each try after, as the SDK's own backoff starts.</summary>
    private static readonly TimeSpan FirstWait = TimeSpan.FromMilliseconds(500);

    /// <summary>The longest wait between tries, as the SDK's own.</summary>
    private static readonly TimeSpan LongestWait = TimeSpan.FromSeconds(8);

    private static readonly ILogger Log = AgentLogging.For<RetryingChatClient>();

    /// <summary>How long to wait before try <c>n</c>: half a second, doubled each try, at most eight.</summary>
    public static TimeSpan WaitBefore(int tryNumber) =>
        TimeSpan.FromTicks(Math.Min(LongestWait.Ticks, FirstWait.Ticks << Math.Min(tryNumber - 1, 10)));

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        for (int tries = 0; ; tries++)
        {
            try
            {
                return await base.GetResponseAsync(messages, options, cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                await AgainOrThrowAsync(failure, tries, spoken: false, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int tries = 0; ; tries++)
        {
            // Updates with no content (a stream's opening) wait here until the first with content, so a retry can
            // drop them and the caller never sees one twice.
            var held = new List<ChatResponseUpdate>();
            bool spoken = false, again = false;
            await using (var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken)
                             .GetAsyncEnumerator(cancellationToken))
            {
                while (true)
                {
                    // A yield can't sit in a try with a catch, so the next update is fetched here and given below.
                    Exception? failed = null;
                    bool more = false;
                    try
                    {
                        more = await updates.MoveNextAsync();
                    }
                    catch (Exception failure) when (failure is not OperationCanceledException)
                    {
                        failed = failure;
                    }

                    if (failed is not null)
                    {
                        await AgainOrThrowAsync(failed, tries, spoken, cancellationToken);
                        again = true;
                        break;
                    }

                    if (!more) break;
                    var update = updates.Current;
                    if (!spoken && update.Contents.Count == 0)
                    {
                        held.Add(update);
                        continue;
                    }

                    if (!spoken)
                    {
                        foreach (var waiting in held) yield return waiting;
                        spoken = true;
                    }

                    yield return update;
                }
            }

            if (again) continue;
            // A stream that ended with nothing but updates without content still gives them, once.
            if (!spoken)
                foreach (var waiting in held)
                    yield return waiting;
            yield break;
        }
    }

    /// <summary>
    /// Waits and returns when the failure is worth asking again; otherwise throws: an account limit as its own
    /// exception, anything else as it was.
    /// </summary>
    private async Task AgainOrThrowAsync(Exception failure, int tries, bool spoken, CancellationToken cancellationToken)
    {
        var named = failures.Of(failure);
        if (named.Kind == ModelFailureKind.AccountLimit) throw new AccountLimitException(named, failure);
        if (named.Kind != ModelFailureKind.Transient || spoken || tries >= maxRetries)
            ExceptionDispatchInfo.Capture(failure).Throw();

        int tryNumber = tries + 1;
        var retried = new ModelAskedAgainHoles(failure.GetType().Name, (int)WaitBefore(tryNumber).TotalMilliseconds,
            tryNumber + 1, maxRetries + 1);
        Log.Log(LogLevel.Warning, LlmLogMessage.ModelAskedAgain, retried);
        await (wait ?? WaitAsync)(tryNumber, cancellationToken);
    }

    /// <summary>The real wait before a try.</summary>
    private static Task WaitAsync(int tryNumber, CancellationToken cancellationToken) =>
        Task.Delay(WaitBefore(tryNumber), cancellationToken);
}
