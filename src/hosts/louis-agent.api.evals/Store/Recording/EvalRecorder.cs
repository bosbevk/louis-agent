using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Logging;
using LouisAgent.Api.Evals.Runs;

namespace LouisAgent.Api.Evals.Store.Recording;

/// <summary>
/// Keeps an eval run in the store while it happens, so a half-done run can already be read. It listens to the run's
/// lines the way the page does and hands each one to the run's recording; a store that fails stops the writes, never
/// the run, and the result says so.
/// </summary>
/// <remarks>
/// Observer: the recorder subscribes to the run's lines (the ones so far, then each new one) instead of the run
/// calling the store, so a run knows nothing about where it's kept. What one run has heard so far lives in
/// <see cref="RunRecording"/>, which was split off when this class passed 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class EvalRecorder(IEvalStore store, ILogger? logger = null, string? ledgerDirectory = null)
{
    /// <summary>Whether everything was kept, and if not, why.</summary>
    public sealed record Result(bool Stored, string? StoreError);

    /// <summary>How many runs' recordings are kept: a page only ever asks about the last few.</summary>
    private const int KeptRecordings = 8;

    /// <summary>Guards the list of recordings, which the endpoints read while runs add to it.</summary>
    private readonly Lock _lock = new();

    /// <summary>The latest runs' recordings, oldest first: what each has heard so far, and its end.</summary>
    private readonly List<(string Run, RunRecording Heard, Task<Result> Recording)> _recordings = [];

    /// <summary>Starts recording the run in the background, so the endpoint that started it doesn't wait.</summary>
    public void Attach(RunningEval run, IReadOnlyDictionary<string, string>? settings = null)
    {
        var heard = new RunRecording(run, settings, store, logger, ledgerDirectory, CancellationToken.None);
        var recording = FoldUntilEndAsync(run, heard, CancellationToken.None);
        lock (_lock)
        {
            _recordings.Add((run.Id, heard, recording));
            if (_recordings.Count > KeptRecordings) _recordings.RemoveAt(0);
        }
    }

    /// <summary>The recording of a run started here, while it's still one of the last few.</summary>
    public Task<Result>? RecordingOf(string id)
    {
        lock (_lock) return _recordings.LastOrDefault(r => r.Run == id).Recording;
    }

    /// <summary>
    /// What a run started here has asked the model so far, from the ledger, each request on its turn (FX34); nothing
    /// for a run this recorder isn't keeping.
    /// </summary>
    public IReadOnlyList<EvalDocuments.Request>? RequestsSoFar(string id)
    {
        RunRecording? heard;
        lock (_lock) heard = _recordings.LastOrDefault(r => r.Run == id).Heard;
        return heard?.RequestsSoFar();
    }

    /// <summary>Records the run until it ends; the settings are what it ran with.</summary>
    public Task<Result> RecordAsync(RunningEval run, IReadOnlyDictionary<string, string>? settings = null,
        CancellationToken cancellationToken = default) =>
        FoldUntilEndAsync(run, new RunRecording(run, settings, store, logger, ledgerDirectory, cancellationToken),
            cancellationToken);

    /// <summary>Folds every line of the run into its recording, then closes it.</summary>
    private static async Task<Result> FoldUntilEndAsync(RunningEval run, RunRecording recording,
        CancellationToken cancellationToken)
    {
        // Listen before the first await, so no line printed meanwhile is missed.
        var (soFar, live) = run.Listen();
        await recording.SaveRunAsync();

        foreach (string line in soFar) await recording.FoldAsync(line);
        await foreach (string line in live.ReadAllAsync(cancellationToken)) await recording.FoldAsync(line);

        return await recording.CloseAsync();
    }
}
