using LouisAgent.Core.Llm.Failures;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Failures;

/// <summary>
/// The retrying client (F8): a request that fails before any words, in a way its provider calls worth it, is asked
/// again, up to the limit; once content has gone out it never is, so nothing is said twice; an account limit is
/// never asked again and becomes its own exception; any other failure is passed on as it was.
/// </summary>
public class RetryingChatClientTests
{
    /// <summary>A busy moment, as a provider would name one.</summary>
    private sealed class Busy : Exception;

    /// <summary>An account limit, as a provider would name one.</summary>
    private sealed class Limit : Exception;

    /// <summary>
    /// The provider's naming: Busy is worth asking again, Limit is an account limit, anything else neither.
    /// </summary>
    private sealed class TestFailures : IModelFailures
    {
        public ModelFailure Of(Exception failure) => failure switch
        {
            Busy => ModelFailure.Transient,
            Limit => ModelFailure.AccountLimit("You have reached your specified API usage limits.", null),
            _ => ModelFailure.Other
        };
    }

    /// <summary>
    /// A model that plays one script per request: each script is the updates it streams, and a failure where an
    /// update should be. It counts the requests.
    /// </summary>
    private sealed class ScriptedModel(params object[][] scripts) : IChatClient
    {
        public int Requests { get; private set; }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            var script = scripts[Requests++];
            foreach (var step in script)
            {
                await Task.Yield();
                if (step is Exception failure) throw failure;
                yield return (ChatResponseUpdate)step;
            }
        }

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            await GetStreamingResponseAsync(messages, options, cancellationToken)
                .ToChatResponseAsync(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>An update with words in it.</summary>
    private static ChatResponseUpdate Words(string text) => new(ChatRole.Assistant, text);

    /// <summary>An update with no content, as a stream's opening event becomes.</summary>
    private static ChatResponseUpdate Opening() => new()
    {
        Role = ChatRole.Assistant,
        ResponseId = "msg_1"
    };

    /// <summary>The client under test over the model, asking again at most twice, with no wait between tries.</summary>
    private static RetryingChatClient Retrying(IChatClient model) =>
        new(model, new TestFailures(), maxRetries: 2, wait: (_, _) => Task.CompletedTask);

    /// <summary>Everything a streamed request gives back, or the failure it ends with.</summary>
    private static async Task<(List<ChatResponseUpdate> Updates, Exception? Failure)> StreamAsync(IChatClient client)
    {
        var updates = new List<ChatResponseUpdate>();
        try
        {
            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
                updates.Add(update);
        }
        catch (Exception failure)
        {
            return (updates, failure);
        }

        return (updates, null);
    }

    /// <summary>
    /// A busy moment before any words, even after the stream's opening event, is asked again, and the opening is not
    /// given twice.
    /// </summary>
    [Test]
    public async Task ABusyMomentBeforeAnyWords_IsAskedAgain_AndNothingIsGivenTwice()
    {
        var model = new ScriptedModel([Opening(), new Busy()], [Opening(), Words("ok")]);

        var (updates, failure) = await StreamAsync(Retrying(model));

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.Null);
            Assert.That(model.Requests, Is.EqualTo(2));
            Assert.That(updates.Count(u => u.ResponseId == "msg_1"), Is.EqualTo(1), "the opening once");
            Assert.That(string.Concat(updates.Select(u => u.Text)), Is.EqualTo("ok"));
        });
    }

    /// <summary>Once words have gone out, a failure is never asked again: it would say them twice.</summary>
    [Test]
    public async Task AFailureAfterWords_IsNeverAskedAgain()
    {
        var model = new ScriptedModel([Words("par"), new Busy()], [Words("never")]);

        var (updates, failure) = await StreamAsync(Retrying(model));

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.TypeOf<Busy>());
            Assert.That(model.Requests, Is.EqualTo(1));
            Assert.That(string.Concat(updates.Select(u => u.Text)), Is.EqualTo("par"));
        });
    }

    /// <summary>A busy moment is asked again only up to the limit, then passed on.</summary>
    [Test]
    public async Task ABusyMoment_IsAskedAgainOnlyUpToTheLimit()
    {
        var model = new ScriptedModel([new Busy()], [new Busy()], [new Busy()], [Words("too late")]);

        var (_, failure) = await StreamAsync(Retrying(model));

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.TypeOf<Busy>());
            Assert.That(model.Requests, Is.EqualTo(3), "the first try and two more");
        });
    }

    /// <summary>
    /// An account limit is never asked again, and becomes its own exception with the provider's reason.
    /// </summary>
    [Test]
    public async Task AnAccountLimit_IsNeverAskedAgain_AndIsItsOwnException()
    {
        var model = new ScriptedModel([new Limit()], [Words("never")]);

        var (_, failure) = await StreamAsync(Retrying(model));

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.TypeOf<AccountLimitException>());
            Assert.That(failure!.Message, Does.Contain("usage limits"));
            Assert.That(failure.InnerException, Is.TypeOf<Limit>(), "the provider's own failure inside");
            Assert.That(model.Requests, Is.EqualTo(1));
        });
    }

    /// <summary>Any other failure is passed on as it was, never asked again.</summary>
    [Test]
    public async Task AnyOtherFailure_IsPassedOnAsItWas()
    {
        var model = new ScriptedModel([new InvalidOperationException("bad request")], [Words("never")]);

        var (_, failure) = await StreamAsync(Retrying(model));

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(model.Requests, Is.EqualTo(1));
        });
    }

    /// <summary>A request that isn't streamed is asked again the same way.</summary>
    [Test]
    public async Task ARequestNotStreamed_IsAskedAgainTheSameWay()
    {
        var model = new ScriptedModel([new Busy()], [Words("ok")]);

        var response = await Retrying(model).GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Multiple(() =>
        {
            Assert.That(response.Text, Is.EqualTo("ok"));
            Assert.That(model.Requests, Is.EqualTo(2));
        });
    }
}
