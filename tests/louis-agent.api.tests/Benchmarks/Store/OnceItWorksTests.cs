using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

// Found checking the review of PR #55: MongoBenchmarkStore made its indexes through a Lazy<Task>, which keeps a failed
// task for ever. Puls started while puls-mongo was still coming up, the first call failed, and every later one threw
// that same failure until Puls was restarted. The store's setup runs once it works, and again after it fails.
public class OnceItWorksTests
{
    [Test]
    public async Task AFailure_IsTriedAgain_ASuccessIsKept()
    {
        int calls = 0;
        var once = new OnceItWorks(() => ++calls == 1 ? Task.FromException(new TimeoutException("Mongo is still starting")) : Task.CompletedTask);

        Assert.ThrowsAsync<TimeoutException>(() => once.RunAsync());
        await once.RunAsync();
        await once.RunAsync();

        Assert.That(calls, Is.EqualTo(2), "tried again after the failure, then kept the success");
    }

    [Test]
    public async Task CallersAtTheSameTime_ShareOneAttempt()
    {
        int calls = 0;
        var gate = new TaskCompletionSource();
        var once = new OnceItWorks(() => { calls++; return gate.Task; });

        var first = once.RunAsync();
        var second = once.RunAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.That(calls, Is.EqualTo(1));
    }
}
