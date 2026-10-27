using System.Net.Sockets;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5.1: which exceptions are the infrastructure's, and which failure each is. Anything else is a bug and stays thrown.
public class TurnFailuresTests
{
    [Test]
    public void TheApisRefusalIsAnApiError() =>
        Assert.That(TurnFailures.Of(new EvalApiException(500, "down"), CancellationToken.None),
            Is.EqualTo(TrialFailure.ApiError));

    [Test]
    public void ABrokenConnectionIsATransportFailure()
    {
        Assert.That(TurnFailures.Of(new HttpRequestException("refused", new SocketException()), CancellationToken.None),
            Is.EqualTo(TrialFailure.Transport));
        Assert.That(TurnFailures.Of(new IOException("the stream ended"), CancellationToken.None),
            Is.EqualTo(TrialFailure.Transport));
    }

    [Test]
    public void ATimeoutIsATimeout_WhetherTheClientOrTheTokenSaidSo()
    {
        Assert.That(TurnFailures.Of(new TimeoutException(), CancellationToken.None), Is.EqualTo(TrialFailure.Timeout));
        // HttpClient's own timeout surfaces as a TaskCanceledException while the run's token is still live.
        Assert.That(
            TurnFailures.Of(new TaskCanceledException("timed out", new TimeoutException()), CancellationToken.None),
            Is.EqualTo(TrialFailure.Timeout));
    }

    [Test]
    public void ACancelledRunIsNoFailure()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(TurnFailures.Of(new TaskCanceledException(), source.Token), Is.Null);
        Assert.That(TurnFailures.Of(new OperationCanceledException(source.Token), source.Token), Is.Null);
    }

    [Test]
    public void ABugIsNoFailure() =>
        Assert.That(TurnFailures.Of(new InvalidOperationException("a bug"), CancellationToken.None), Is.Null);
}
