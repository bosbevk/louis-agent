using LouisAgent.Evals.Contracts.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The desk knows the one run that is going, so a stored run that still says running and isn't that one was cut
/// off by a restart, and reads as could not finish with no clean-up job.
/// </summary>
public class RunDeskTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    /// <summary>A stored run that says running, while the desk runs nothing, reads as could not finish.</summary>
    [Test]
    public void AStoredRunningRun_ThatIsntTheDesksOwn_ReadsAsCouldNotFinish()
    {
        var desk = new RunDesk(NullLogger<RunDesk>.Instance);
        var stored = new EvalRun("20261123-043012-greets", "greets", "an-eval-agent", RunKind.Live, 1, Started, []);

        var read = desk.Read(stored);

        Assert.That(read.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(read.StoppedBecause, Is.Not.Empty);
    }

    /// <summary>A run that ended reads as it is.</summary>
    [Test]
    public void AnEndedRun_ReadsAsItIs()
    {
        var desk = new RunDesk(NullLogger<RunDesk>.Instance);
        var stored = new EvalRun("20261123-043012-greets", "greets", "an-eval-agent", RunKind.Live, 1, Started, [])
        {
            Status = RunStatus.Finished
        };

        Assert.That(desk.Read(stored), Is.SameAs(stored));
    }

    /// <summary>F126.3.4: stopping the going run by its id cancels its work, and says it did.</summary>
    [Test]
    public async Task TryStop_WithTheGoingRunsId_CancelsItsWork()
    {
        using var desk = await StartedDeskAsync();
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        desk.TryStart(Run(), signals => WaitAsync(signals, cancelled));
        await Until(() => desk.Current is not null);

        bool stopped = desk.TryStop(Run().Id);

        Assert.That(stopped, Is.True);
        Assert.That(await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.True, "the person's own signal");
    }

    /// <summary>Another id stops nothing, and the work goes on; with nothing going there is nothing to stop.</summary>
    [Test]
    public async Task TryStop_WithAnotherId_OrNothingGoing_StopsNothing()
    {
        using var desk = await StartedDeskAsync();
        Assert.That(desk.TryStop(Run().Id), Is.False, "nothing going");
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        desk.TryStart(Run(), signals => WaitAsync(signals, cancelled));
        await Until(() => desk.Current is not null);

        Assert.That(desk.TryStop("20261123-043013-greets"), Is.False);
        Assert.That(cancelled.Task.IsCompleted, Is.False, "the work goes on");
        desk.TryStop(Run().Id);
    }

    /// <summary>After a stopped run's work ends, the slot is free for the next run.</summary>
    [Test]
    public async Task AfterAStoppedRun_TheSlotIsFree()
    {
        using var desk = await StartedDeskAsync();
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        desk.TryStart(Run(), signals => WaitAsync(signals, cancelled));
        await Until(() => desk.Current is not null);

        desk.TryStop(Run().Id);
        await Until(() => desk.Current is null);

        Assert.That(desk.TryStart(Run() with { Id = "20261123-050000-greets" }, _ => Task.CompletedTask), Is.True);
    }

    /// <summary>A desk started as the host starts it.</summary>
    private static async Task<RunDesk> StartedDeskAsync()
    {
        var desk = new RunDesk(NullLogger<RunDesk>.Instance);
        await desk.StartAsync(CancellationToken.None);
        return desk;
    }

    /// <summary>A run's work that waits until it is cancelled, and says whether the person's signal was.</summary>
    private static async Task WaitAsync(RunSignals signals, TaskCompletionSource<bool> cancelled)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, signals.Playing);
        }
        catch (OperationCanceledException)
        {
            cancelled.SetResult(signals.Stopped.IsCancellationRequested);
            throw;
        }
    }

    /// <summary>Waits a little at a time until the condition holds, for at most five seconds.</summary>
    private static async Task Until(Func<bool> holds)
    {
        for (int tries = 0; tries < 250 && !holds(); tries++) await Task.Delay(20);
        Assert.That(holds(), Is.True, "the desk never got there");
    }

    /// <summary>A run of the test eval as it starts.</summary>
    private static EvalRun Run() =>
        new("20261123-043012-greets", "greets", "an-eval-agent", RunKind.Live, 1, Started, []);
}
