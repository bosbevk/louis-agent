using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

/// <summary>
/// The store's one-time setup runs again after a failure and only stops once it has worked. The Mongo store used to
/// keep a failed setup for ever: Puls started while its Mongo was still coming up, the first call failed, and every
/// later one threw that same failure until Puls was restarted.
/// </summary>
public class OnceItWorksTests
{
    /// <summary>A failed attempt is tried again on the next call, and a successful one isn't repeated.</summary>
    [Test]
    public async Task AFailure_IsTriedAgain_ASuccessIsKept()
    {
        // The first attempt fails as if Mongo were still starting; every later one works.
        int calls = 0;
        var once = new OnceItWorks(() => ++calls == 1
            ? Task.FromException(new TimeoutException("Mongo is still starting"))
            : Task.CompletedTask);

        Assert.ThrowsAsync<TimeoutException>(() => once.RunAsync());
        await once.RunAsync();
        await once.RunAsync();

        Assert.That(calls, Is.EqualTo(2), "tried again after the failure, then kept the success");
    }

    /// <summary>Two callers that arrive while the setup is still running wait for the same attempt.</summary>
    [Test]
    public async Task CallersAtTheSameTime_ShareOneAttempt()
    {
        // The setup only finishes when we open the gate, so both callers arrive while it runs.
        int calls = 0;
        var gate = new TaskCompletionSource();
        var once = new OnceItWorks(() =>
        {
            calls++;
            return gate.Task;
        });

        var first = once.RunAsync();
        var second = once.RunAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.That(calls, Is.EqualTo(1));
    }
}
