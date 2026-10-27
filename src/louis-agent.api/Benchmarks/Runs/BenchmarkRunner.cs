using System.Diagnostics;
using System.Runtime.CompilerServices;
using LouisAgent.Api.Config;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Runs a check as a child process (F27): the eval runner with an argument list, never a shell string, in the
/// workspace, with <c>PULS_API</c> pointing at this API and <c>BENCHMARK_RUN</c> its id (<see cref="RunnerStartInfo"/> builds it); a run's results go to the
/// benchmark store through <c>BenchmarkRecorder</c> (F31). Every check is an eval since F18 step 7.4: no script, no interpreter. One run at a time: the containers are shared, and a second run would skew the first's
/// ledger and timings. Its lines (stdout and stderr) are kept on the run and streamed to listeners.
/// </summary>
/// <param name="evalHost">What runs the eval runner's dll: <c>dotnet</c>; a test gives a stand-in.</param>
public sealed class BenchmarkRunner(
    BenchmarkCatalog catalog,
    string workspaceRoot,
    string pulsApi,
    string evalRunner = AdminOptions.DefaultEvalRunner,
    string evalHost = "dotnet")
{
    /// <summary>Repeats one run may ask for.</summary>
    public const int MaxRepeat = 5;

    private readonly Lock _lock = new();
    private readonly RunnerPaths _paths = new(workspaceRoot, pulsApi, evalRunner, evalHost);

    /// <summary>The run in progress, or the last one; null before the first.</summary>
    public BenchmarkRun? Current { get; private set; }

    public bool IsRunning => Current?.Status == BenchmarkRunStatus.Running;

    /// <summary>Starts a run; null when the check is unknown or one is already running. <paramref name="options"/> say how an eval handles the web (F18 step 5.5).</summary>
    public BenchmarkRun? Start(string checkId, int repeat, BenchmarkRunOptions? options = null)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == checkId) is not { } check) return null;
        return StartLocked(check, new BenchmarkRun(NewId(), check.Id, Math.Clamp(repeat, 1, MaxRepeat), options));
    }

    /// <summary>
    /// Starts a re-score of a stored run (F18 step 5b.2): the eval runner with <c>--rescore {rescoreOf}</c>, and the stored run's export on its stdin. The new run keeps the
    /// stored run's repeats (6a.4c: it holds the same conversations, and the explorer offers the repeats this number says). Null when
    /// the check is unknown, is not an eval, or a run is already running. The runner grades; this only starts it and keeps its lines.
    /// </summary>
    public BenchmarkRun? StartRescore(string checkId, string rescoreOf, string export, int repeat)
    {
        if (EvalCheck(checkId) is not { } check) return null;
        return StartLocked(check,
            new BenchmarkRun(NewId(), check.Id, Math.Clamp(repeat, 1, MaxRepeat), rescoreOf: rescoreOf, input: export));
    }

    /// <summary>
    /// Starts a judge check (F18 step 6a.5b): the eval runner with <c>--calibrate {eval} {grader} --labels -</c>, and the people's grades of <paramref name="calibrationOf"/> as
    /// label lines on its stdin. It is a run of the runner's one at a time (the judge is shared), but no benchmark run: nothing records it in the benchmark store, and
    /// <c>CalibrationRecorder</c> keeps its report. Null when the check is unknown, is not an eval, or a run is already running.
    /// </summary>
    public BenchmarkRun? StartCalibration(string checkId, string calibrationOf, string grader, string labels)
    {
        if (EvalCheck(checkId) is not { } check) return null;
        return StartLocked(check,
            new BenchmarkRun(NewId(), check.Id, 1, calibration: new BenchmarkCalibration(calibrationOf, grader),
                input: labels));
    }

    /// <summary>The run's lines: those so far, then each new one until it ends. Empty for an unknown id.</summary>
    public async IAsyncEnumerable<string> EventsAsync(string id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Current is not { } run || run.Id != id) yield break;
        var (soFar, live) = run.Listen();
        foreach (string line in soFar) yield return line;
        await foreach (string line in live.ReadAllAsync(cancellationToken)) yield return line;
    }

    /// <summary>The start of one run's process (F18 step 4.3), as <see cref="RunnerStartInfo"/> builds it with this runner's paths.</summary>
    internal ProcessStartInfo StartInfoFor(BenchmarkCheck check, BenchmarkRun run) =>
        RunnerStartInfo.For(check, run, _paths);

    /// <summary>Writes the stored run's export to the runner's stdin and closes it; a runner that ended without reading it is not an error here, its exit code says how it ended.</summary>
    internal static async Task PumpAsync(TextWriter stdin, string input)
    {
        try
        {
            await stdin.WriteAsync(input);
            stdin.Close();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    private BenchmarkCheck? EvalCheck(string checkId) => catalog.Checks.FirstOrDefault(c => c.Id == checkId);

    private BenchmarkRun? StartLocked(BenchmarkCheck check, BenchmarkRun run)
    {
        lock (_lock)
        {
            if (IsRunning) return null;
            Current = run;
            Launch(run, check);
            return run;
        }
    }

    private void Launch(BenchmarkRun run, BenchmarkCheck check)
    {
        var start = StartInfoFor(check, run);

        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) run.Add(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) run.Add(e.Data);
        };
        process.Exited += (_, _) =>
        {
            // The stream readers may still be draining when Exited fires; wait for them so the last lines arrive first.
            process.WaitForExit();
            run.Complete(process.ExitCode);
            process.Dispose();
        };
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // On its own task: a runner slow to read its stdin (or one that never does) must not hold the start, which holds the runner's lock.
            if (run.Input is { } input) _ = Task.Run(() => PumpAsync(process.StandardInput, input));
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            run.Add(ex.Message);
            run.Complete(-1);
        }
    }
}
