using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Benchmarks.Calibrations;

/// <summary>
/// Keeps the report of a judge check. It listens to the run's lines and, when the run ended well, stores its one
/// <c>calibration</c> line against the run whose labels were checked.
/// </summary>
/// <remarks>
/// A run that failed, or printed no report, stores nothing and the result says why, so a half-finished run never
/// reads as a verdict. A store that fails is logged, never thrown at the run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text lives in enum templates</seealso>
public sealed class CalibrationRecorder(ICalibrationStore store, ILogger? logger = null)
{
    /// <summary>Whether a report was kept, and if not, why.</summary>
    public sealed record Result(bool Stored, string? Error);

    /// <summary>How many runs' recordings we remember: a page only ever asks about the last few.</summary>
    private const int KeptRecordings = 8;

    private readonly Lock _lock = new();
    private readonly List<(string Run, Task<Result> Recording)> _recordings = [];

    /// <summary>Starts recording a run in the background, so the endpoint that started it needn't wait.</summary>
    public void Attach(BenchmarkRun run)
    {
        var recording = RecordAsync(run);

        // Remember it for a page to ask about, and forget the oldest once there are too many.
        lock (_lock)
        {
            _recordings.Add((run.Id, recording));
            if (_recordings.Count > KeptRecordings) _recordings.RemoveAt(0);
        }
    }

    /// <summary>The recording of a run we started, if we still remember it.</summary>
    public Task<Result>? RecordingOf(string id)
    {
        lock (_lock) return _recordings.LastOrDefault(r => r.Run == id).Recording;
    }

    /// <summary>Follows the run to its end and stores its report, or says why there is none.</summary>
    /// <remarks>
    /// Why there is no report is a switch expression on the shape (the exit code, the report), one arm per case, read
    /// like a table, instead of a ternary inside a ternary.
    /// </remarks>
    public async Task<Result> RecordAsync(BenchmarkRun run, CancellationToken cancellationToken = default)
    {
        if (run.Calibration is not { } calibration)
            return new Result(false, MessageText.Format(ApiUserText.CalibrationNotACheck, new { Run = run.Id }));

        // Listen before the first await, so no line printed meanwhile is missed.
        var (soFar, live) = run.Listen();
        CalibrationRecord? report = null;
        string? last = null;

        // Keeps the last line for the error, and the latest report the lines held.
        void Fold(string line)
        {
            if (!string.IsNullOrWhiteSpace(line)) last = line;
            // The line's time is the run's end, which isn't known yet: the record is remade with it below.
            report = CalibrationLineReader.Read(line, calibration.Of, DateTimeOffset.UtcNow) ?? report;
        }

        foreach (string line in soFar) Fold(line);
        await foreach (string line in live.ReadAllAsync(cancellationToken)) Fold(line);

        // A run that didn't exit cleanly, or that printed no report, has nothing to store.
        string? reason = (run.ExitCode, report) switch
        {
            (not 0, _) => MessageText.Format(ApiUserText.CalibrationRunFailed,
                new
                {
                    Code = run.ExitCode,
                    Last = last ?? ""
                }),
            (_, null) => MessageText.Format(ApiUserText.CalibrationNoReport, new { Last = last ?? "" }),
            _ => null,
        };
        if (reason is null)
        {
            // Store it with the run's real end time. A store that fails becomes the reason, never an exception.
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

        logger?.Log(LogLevel.Warning, ApiLogMessage.CalibrationNotStored,
            new
            {
                Run = calibration.Of,
                calibration.Grader,
                Reason = reason
            }, tag: "API");
        return new Result(false, reason);
    }
}
