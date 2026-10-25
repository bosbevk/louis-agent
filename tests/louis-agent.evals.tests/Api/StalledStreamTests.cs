using System.Diagnostics;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Api;

// F18 step 5.1 review: a stream that stalls after its headers must not hang the runner. The Python's 600 seconds was per read; here
// it is an idle limit that restarts after each event. Real sockets, because only they have headers that arrived and a body that did not.
public class StalledStreamTests
{
    private static readonly TimeSpan None = TimeSpan.Zero;

    /// <summary>The test's own limit (CancelAfter): the run's token here, so a client without the idle limit fails the test in 15 seconds instead of hanging it.</summary>
    private static CancellationToken Run => TestContext.CurrentContext.CancellationToken;

    private static HttpSessionClient ClientTo(SlowSseServer server, TimeSpan httpTimeout, TimeSpan idle)
    {
        var http = new HttpClient { BaseAddress = server.Address, Timeout = httpTimeout };
        return new HttpSessionClient(http, idle);
    }

    [Test, CancelAfter(15_000)]
    public async Task LearningTest_HttpClientTimeoutStopsCountingOnceTheHeadersHaveArrived()
    {
        // Pins the library, not our code: with ResponseHeadersRead, HttpClient.Timeout covers the wait for the headers only. If a future
        // .NET changes that, this fails, and the idle limit in SendAsync may be redundant.
        await using var server = new SlowSseServer((None, SlowSseServer.Event("een")));
        using var http = new HttpClient { BaseAddress = server.Address, Timeout = TimeSpan.FromMilliseconds(400) };
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/sessions/s/messages") { Content = new StringContent("{}") };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        await using var body = await response.Content.ReadAsStreamAsync();
        var first = await SessionStreamReader.ReadAsync(body, CancellationToken.None).FirstAsync();
        using var ours = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
        var clock = Stopwatch.StartNew();
        var next = Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in SessionStreamReader.ReadAsync(body, ours.Token)) { }
        });

        Assert.That(first.Type, Is.EqualTo("content_block_delta"));
        Assert.That(next!.CancellationToken, Is.EqualTo(ours.Token), "it was our token that stopped the read, not HttpClient's 400 ms timeout");
        Assert.That(clock.Elapsed, Is.GreaterThan(TimeSpan.FromMilliseconds(1000)));
    }

    [Test, CancelAfter(15_000)]
    public async Task AStreamThatGoesSilent_EndsTheTurnAsATimeout_AfterTheIdleLimit()
    {
        await using var server = new SlowSseServer((None, SlowSseServer.Event("een")));
        var client = ClientTo(server, TimeSpan.FromSeconds(30), idle: TimeSpan.FromMilliseconds(400));
        var events = new List<SessionEvent>();

        var stalled = Assert.CatchAsync<Exception>(async () =>
        {
            await foreach (var streamEvent in client.SendAsync("s", "Hallo", Run)) events.Add(streamEvent);
        });

        Assert.That(events, Has.Count.EqualTo(1), "the event read before the silence is kept");
        Assert.That(TurnFailures.Of(stalled!, Run), Is.EqualTo(TrialFailure.Timeout));
    }

    [Test, CancelAfter(15_000)]
    public async Task EachEventRestartsTheIdleClock_SoALongTurnOfLiveEventsIsNotCutOff()
    {
        var wait = TimeSpan.FromMilliseconds(250);
        await using var server = new SlowSseServer(Enumerable.Range(1, 6).Select(n => (wait, SlowSseServer.Event("n" + n))).ToArray());
        var client = ClientTo(server, TimeSpan.FromSeconds(30), idle: TimeSpan.FromMilliseconds(700));
        var clock = Stopwatch.StartNew();
        int seen = 0;

        // Six events 250 ms apart take 1.5 s, longer than the 700 ms idle limit; the silence after the sixth then ends the turn.
        var ended = Assert.CatchAsync<Exception>(async () =>
        {
            await foreach (var _ in client.SendAsync("s", "Hallo", Run)) seen++;
        });

        Assert.That(seen, Is.EqualTo(6));
        Assert.That(clock.Elapsed, Is.GreaterThan(TimeSpan.FromMilliseconds(1500)));
        Assert.That(TurnFailures.Of(ended!, Run), Is.EqualTo(TrialFailure.Timeout));
    }

    [Test, CancelAfter(15_000)]
    public async Task TheCallersOwnTimeBetweenEvents_IsNotCountedAgainstTheStream()
    {
        // The idle limit is the stream's silence, not the caller's work: the runner writes protocol lines between events.
        await using var server = new SlowSseServer((None, SlowSseServer.Event("een")), (TimeSpan.FromMilliseconds(100), SlowSseServer.Event("twee")));
        var client = ClientTo(server, TimeSpan.FromSeconds(30), idle: TimeSpan.FromMilliseconds(300));
        var seen = new List<string>();

        Assert.CatchAsync<Exception>(async () =>
        {
            await foreach (var streamEvent in client.SendAsync("s", "Hallo", Run))
            {
                seen.Add(streamEvent.Type);
                if (seen.Count == 1) await Task.Delay(700, Run);   // slower than the idle limit
            }
        });

        Assert.That(seen, Has.Count.EqualTo(2), "the second event was waiting; the caller's 700 ms was not the stream's silence");
    }

    [Test, CancelAfter(15_000)]
    public async Task ACancelledRunIsNoTimeout()
    {
        await using var server = new SlowSseServer((None, SlowSseServer.Event("een")));
        var client = ClientTo(server, TimeSpan.FromSeconds(30), idle: TimeSpan.FromSeconds(30));
        using var run = new CancellationTokenSource();

        var cancelled = Assert.CatchAsync<Exception>(async () =>
        {
            await foreach (var _ in client.SendAsync("s", "Hallo", run.Token)) await run.CancelAsync();
            await Task.Delay(Timeout.Infinite, run.Token);
        });

        Assert.That(TurnFailures.Of(cancelled!, run.Token), Is.Null);
    }
}
