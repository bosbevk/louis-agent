using LouisAgent.Api.Evals.Messages;
using LouisAgent.Api.Evals.Messages.Holes;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Contracts.Runs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Evals;

/// <summary>
/// Holds the one run that is going, and runs it in the background of the host after the request that started it
/// was answered. A second start while one goes is refused, and the host waits for the run when it shuts down.
/// </summary>
/// <remarks>
/// A hosted service (Microsoft's <c>BackgroundService</c>), the queued background tasks of its guidance cut to one
/// slot that refuses instead of waiting, chosen over the guidance's general queue and over a bare
/// <c>Task.Run</c> that nobody awaits (Louis, 2026-11-23). Taking the slot is one step under a lock, so two starts
/// at the same moment can't both win. A run cut off by a shutdown stays running in its file, and
/// <see cref="Read"/> says it could not finish, so no clean-up job is needed.
/// </remarks>
/// <param name="log">Where a run that failed is logged.</param>
/// <seealso href="https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services">Background tasks with hosted services in ASP.NET Core</seealso>
public sealed class RunDesk(ILogger<RunDesk> log) : BackgroundService
{
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _handed = new(0, 1);
    private Going? _going;

    /// <summary>The run that is going, as it started, or none.</summary>
    public EvalRun? Current
    {
        get
        {
            lock (_gate) return _going?.Run;
        }
    }

    /// <summary>Takes the slot for this run and hands its work to the background, unless a run is going.</summary>
    public bool TryStart(EvalRun started, Func<CancellationToken, Task> work)
    {
        lock (_gate)
        {
            if (_going is not null) return false;
            _going = new Going(started, work);
        }

        _handed.Release();
        return true;
    }

    /// <summary>
    /// The stored run as it really is: one that says running and isn't the run going here was cut off, and could not
    /// finish.
    /// </summary>
    public EvalRun Read(EvalRun stored)
    {
        if (stored.Status != RunStatus.Running) return stored;
        if (Current?.Id == stored.Id) return stored;

        return stored with
        {
            Status = RunStatus.CouldNotFinish,
            StoppedBecause = MessageText.Format(EvalsApiMessage.CutOff)
        };
    }

    /// <summary>Runs each run handed over, one at a time, until the host stops.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _handed.WaitAsync(stoppingToken);
            await RunAsync(_going!, stoppingToken);
        }
    }

    /// <summary>
    /// One run's work, awaited here so a failure is never lost: it is logged, and the slot is free again whatever
    /// happened. A shutdown's cancellation leaves the run in its file as it was.
    /// </summary>
    private async Task RunAsync(Going going, CancellationToken stoppingToken)
    {
        try
        {
            await going.Work(stoppingToken);
        }
        catch (Exception failed) when (failed is not OperationCanceledException ||
                                       !stoppingToken.IsCancellationRequested)
        {
            var runFailed = new RunFailedHoles(going.Run.Id, failed.Message);
            log.Log(LogLevel.Error, EvalsApiMessage.RunFailed, runFailed, failed);
        }
        finally
        {
            lock (_gate) _going = null;
        }
    }

    /// <summary>The run that is going, and the work that plays it.</summary>
    private sealed record Going(EvalRun Run, Func<CancellationToken, Task> Work);
}
