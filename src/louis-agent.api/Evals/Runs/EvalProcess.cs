using System.Diagnostics;
using System.Runtime.CompilerServices;
using LouisAgent.Api.Config;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Start;

namespace LouisAgent.Api.Evals.Runs;

/// <summary>
/// Starts an eval as a child process in the workspace and keeps what it prints, so pages can follow along. Only one
/// runs at a time: the containers are shared, and a second run would skew the first one's ledger and timings.
/// </summary>
/// <remarks>
/// The process gets an argument list, never a shell string, with <c>PULS_API</c> pointing at this API and
/// <c>BENCHMARK_RUN</c> set to the run's id (<see cref="RunnerStartInfo"/> builds it). Saving the results to the store
/// is someone else's job (<c>BenchmarkRecorder</c>); this class only starts the run and keeps its lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
/// <param name="evalHost">What runs the eval runner's dll: <c>dotnet</c>; a test gives a stand-in.</param>
public sealed class EvalProcess(
    EvalList catalog,
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
    public RunningEval? Current { get; private set; }

    public bool IsRunning => Current?.Status == EvalRunStatus.Running;

    /// <summary>
    /// Starts a run of an eval, with how it should treat the web. You get nothing back if the eval is unknown or
    /// another run is still going.
    /// </summary>
    public RunningEval? Start(string checkId, int repeat, EvalRunOptions? options = null)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == checkId) is not { } check) return null;
        return StartLocked(check, new RunningEval(NewId(), check.Id, Math.Clamp(repeat, 1, MaxRepeat), options));
    }

    /// <summary>
    /// Grades a stored run again without asking the model: the runner gets <c>--rescore {rescoreOf}</c> and reads the
    /// stored run's export from its input. The new run keeps the stored run's repeats, since it holds the same
    /// conversations. You get nothing back if the eval is unknown or another run is still going.
    /// </summary>
    public RunningEval? StartRescore(string checkId, string rescoreOf, string export, int repeat)
    {
        if (EvalCheck(checkId) is not { } check) return null;
        return StartLocked(check,
            new RunningEval(NewId(), check.Id, Math.Clamp(repeat, 1, MaxRepeat), rescoreOf: rescoreOf, input: export));
    }

    /// <summary>
    /// Checks a judge against what people decided: the runner gets <c>--calibrate {eval} {grader} --labels -</c> and
    /// reads the people's grades of <paramref name="calibrationOf"/> from its input. It waits its turn like any run
    /// (the judge is shared), but it isn't saved as one: <c>CalibrationRecorder</c> keeps its report. You get nothing
    /// back if the eval is unknown or another run is still going.
    /// </summary>
    public RunningEval? StartCalibration(string checkId, string calibrationOf, string grader, string labels)
    {
        if (EvalCheck(checkId) is not { } check) return null;
        return StartLocked(check,
            new RunningEval(NewId(), check.Id, 1, calibration: new EvalCalibration(calibrationOf, grader),
                input: labels));
    }

    /// <summary>The run's lines: those so far, then each new one until it ends. Nothing for an unknown id.</summary>
    public async IAsyncEnumerable<string> EventsAsync(string id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Current is not { } run || run.Id != id) yield break;

        var (soFar, live) = run.Listen();
        foreach (string line in soFar) yield return line;
        await foreach (string line in live.ReadAllAsync(cancellationToken)) yield return line;
    }

    /// <summary>How the process for one run is started, with this runner's paths.</summary>
    internal ProcessStartInfo StartInfoFor(EvalEntry check, RunningEval run) =>
        RunnerStartInfo.For(check, run, _paths);

    /// <summary>
    /// Feeds the runner its input and closes it. If the runner ended without reading it, that's fine here: its exit
    /// code says how it went.
    /// </summary>
    internal static async Task PumpAsync(TextWriter stdin, string input)
    {
        try
        {
            await stdin.WriteAsync(input);
            stdin.Close();
        }
        // A runner that already quit closes the pipe under us; nothing to do about it.
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
        }
    }

    /// <summary>A short, fresh run id.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>The eval with this name, if the catalogue has it.</summary>
    private EvalEntry? EvalCheck(string checkId) => catalog.Checks.FirstOrDefault(c => c.Id == checkId);

    /// <summary>Starts the run unless another is still going. The lock stops two sneaking in at once.</summary>
    private RunningEval? StartLocked(EvalEntry check, RunningEval run)
    {
        lock (_lock)
        {
            if (IsRunning) return null;

            Current = run;
            Launch(run, check);
            return run;
        }
    }

    /// <summary>
    /// Starts the process and wires up its output to the run. If it can't even start, the run ends at once with the
    /// reason as its only line.
    /// </summary>
    private void Launch(RunningEval run, EvalEntry check)
    {
        var start = StartInfoFor(check, run);

        // Both output and errors become lines of the run, in the order they arrive.
        var process = new Process
        {
            StartInfo = start,
            EnableRaisingEvents = true,
        };
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
            // The stream readers may still be draining when Exited fires; wait for them so the last lines arrive
            // first.
            process.WaitForExit();
            run.Complete(process.ExitCode);
            process.Dispose();
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // On its own task: a runner slow to read its input (or one that never does) must not hold the start,
            // which holds the runner's lock.
            if (run.Input is { } input) _ = Task.Run(() => PumpAsync(process.StandardInput, input));
        }
        // No such program, or it couldn't start: say why on the run and end it.
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            run.Add(ex.Message);
            run.Complete(-1);
        }
    }
}
