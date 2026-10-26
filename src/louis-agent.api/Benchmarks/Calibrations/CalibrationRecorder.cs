using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Calibrations;

/// <summary>
/// Keeps a judge check's report (F18 step 6a.5b): it listens to the run's lines as the benchmark recorder does (<see cref="BenchmarkRun.Listen"/>: the lines so far, then each new
/// one) and, when the run has ended well, stores the one <c>calibration</c> line as a <see cref="CalibrationRecord"/> for the run whose labels were checked. A run that failed, or
/// printed no such line, stores nothing and the result says why, so a half-run never reads as a verdict; a store that fails is logged, never thrown at the run.
/// </summary>
public sealed class CalibrationRecorder(ICalibrationStore store, ILogger? logger = null)
{
    /// <summary>Whether a report was kept; <see cref="Error"/> says why not.</summary>
    public sealed record Result(bool Stored, string? Error);

    /// <summary>How many runs' recordings are kept for <see cref="RecordingOf"/>: the last few are all a page asks about.</summary>
    private const int KeptRecordings = 8;

    private readonly Lock _lock = new();
    private readonly List<(string Run, Task<Result> Recording)> _recordings = [];

    /// <summary>Starts recording <paramref name="run"/> in the background (the endpoint that started it doesn't wait).</summary>
    public void Attach(BenchmarkRun run)
    {
        var recording = RecordAsync(run);
        lock (_lock)
        {
            _recordings.Add((run.Id, recording));
            if (_recordings.Count > KeptRecordings) _recordings.RemoveAt(0);
        }
    }

    /// <summary>The recording of a run <see cref="Attach"/> started; null for one it didn't (or no longer keeps).</summary>
    public Task<Result>? RecordingOf(string id)
    {
        lock (_lock) return _recordings.LastOrDefault(r => r.Run == id).Recording;
    }

    public async Task<Result> RecordAsync(BenchmarkRun run, CancellationToken cancellationToken = default)
    {
        if (run.Calibration is not { } calibration) return new Result(false, MessageText.Format(ApiUserText.CalibrationNotACheck, new { Run = run.Id }));

        // Listen before the first await, so no line printed meanwhile is missed.
        var (soFar, live) = run.Listen();
        CalibrationRecord? report = null;
        string? last = null;
        void Fold(string line)
        {
            if (!string.IsNullOrWhiteSpace(line)) last = line;
            // The line's time is the run's end, which isn't known yet: the record is remade with it below.
            report = CalibrationLineReader.Read(line, calibration.Of, DateTimeOffset.UtcNow) ?? report;
        }
        foreach (string line in soFar) Fold(line);
        await foreach (string line in live.ReadAllAsync(cancellationToken)) Fold(line);

        string? reason = run.ExitCode != 0
            ? MessageText.Format(ApiUserText.CalibrationRunFailed, new { Code = run.ExitCode, Last = last ?? "" })
            : report is null ? MessageText.Format(ApiUserText.CalibrationNoReport, new { Last = last ?? "" }) : null;
        if (reason is null)
        {
            try
            {
                await store.AddAsync(report! with { At = run.EndedAt ?? DateTimeOffset.UtcNow }, cancellationToken);
                return new Result(true, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                reason = ex.Message;
            }
        }
        logger?.Log(LogLevel.Warning, ApiLogMessage.CalibrationNotStored, new { Run = calibration.Of, calibration.Grader, Reason = reason }, tag: "API");
        return new Result(false, reason);
    }
}
