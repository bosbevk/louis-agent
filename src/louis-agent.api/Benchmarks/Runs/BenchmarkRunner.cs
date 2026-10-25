using System.Diagnostics;
using System.Runtime.CompilerServices;
using LouisAgent.Api.Config;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Runs a check's script as a child process (F27): the interpreter with an argument list, never a shell string, in the
/// workspace, with <c>PULS_API</c> pointing at this API and <c>BENCHMARK_RUN</c> its id; a run's results go to the
/// benchmark store through <c>BenchmarkRecorder</c> (F31). One run at a time: the containers are shared, and a second run would skew the first's
/// ledger and timings. Its lines (stdout and stderr) are kept on the run and streamed to listeners.
/// </summary>
/// <param name="interpreter">The Python to run; <c>python3</c> in the containers.</param>
public sealed class BenchmarkRunner(BenchmarkCatalog catalog, string workspaceRoot, string pulsApi, string interpreter = "python3",
    string evalRunner = AdminOptions.DefaultEvalRunner)
{
    /// <summary>Repeats one run may ask for.</summary>
    public const int MaxRepeat = 5;

    private readonly Lock _lock = new();

    /// <summary>The run in progress, or the last one; null before the first.</summary>
    public BenchmarkRun? Current { get; private set; }

    public bool IsRunning => Current?.Status == BenchmarkRunStatus.Running;

    /// <summary>Starts a run; null when the check is unknown or one is already running. <paramref name="options"/> say how an eval handles the web (F18 step 5.5).</summary>
    public BenchmarkRun? Start(string checkId, int repeat, BenchmarkRunOptions? options = null)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == checkId) is not { } check) return null;
        repeat = Math.Clamp(repeat, 1, MaxRepeat);

        lock (_lock)
        {
            if (IsRunning) return null;
            var run = new BenchmarkRun(Guid.NewGuid().ToString("N")[..12], check.Id, repeat, options);
            Current = run;
            Launch(run, check);
            return run;
        }
    }

    /// <summary>
    /// Starts a re-score of a stored run (F18 step 5b.2): the eval runner with <c>--rescore {rescoreOf}</c>, and the stored run's export on its stdin. Null when
    /// the check is unknown, is not an eval, or a run is already running. The runner grades; this only starts it and keeps its lines.
    /// </summary>
    public BenchmarkRun? StartRescore(string checkId, string rescoreOf, string export)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == checkId) is not { Kind: BenchmarkCheckKind.Eval } check) return null;

        lock (_lock)
        {
            if (IsRunning) return null;
            var run = new BenchmarkRun(Guid.NewGuid().ToString("N")[..12], check.Id, 1, rescoreOf: rescoreOf, input: export);
            Current = run;
            Launch(run, check);
            return run;
        }
    }

    /// <summary>The run's lines: those so far, then each new one until it ends. Empty for an unknown id.</summary>
    public async IAsyncEnumerable<string> EventsAsync(string id, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Current is not { } run || run.Id != id) yield break;
        var (soFar, live) = run.Listen();
        foreach (string line in soFar) yield return line;
        await foreach (string line in live.ReadAllAsync(cancellationToken)) yield return line;
    }

    /// <summary>
    /// The start of one run's process (F18 step 4.3): an eval's <c>dotnet {EVAL_RUNNER} run {name} --repeat N [--web record | --replay {runId}]</c>, or a script's
    /// interpreter with its arguments. Either way an argument list (ADR 0004), and the same environment.
    /// </summary>
    internal ProcessStartInfo StartInfoFor(BenchmarkCheck check, BenchmarkRun run)
    {
        var start = new ProcessStartInfo(check.Kind == BenchmarkCheckKind.Eval ? "dotnet" : interpreter)
        {
            WorkingDirectory = workspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // A re-score reads the stored run's export on stdin (F18 step 5b.2); any other run reads nothing.
            RedirectStandardInput = run.Input is not null,
            CreateNoWindow = true,
        };
        // Arguments as data (ADR 0004): the runner's or the script's path and its arguments are a list, never a command string.
        if (check.Kind == BenchmarkCheckKind.Eval)
        {
            start.ArgumentList.Add(evalRunner);
            start.ArgumentList.Add("run");
            start.ArgumentList.Add(check.Id);
        }
        else
        {
            start.ArgumentList.Add(check.Script);
        }
        // A re-score takes the repeats, the web and the split from the stored run: its only flag is the run it grades again.
        if (run.RescoreOf is { } rescoreOf)
        {
            start.ArgumentList.Add("--rescore");
            start.ArgumentList.Add(rescoreOf);
        }
        else
        {
            start.ArgumentList.Add("--repeat");
            start.ArgumentList.Add(run.Repeat.ToString());
        }
        // The web (F18 step 5.5), an eval's alone: a recording, or a replay of another run with the model's answers too (--replay). Never a script's.
        if (check.Kind == BenchmarkCheckKind.Eval && run.RescoreOf is null)
        {
            if (run.Options.Replay is { } replayOf)
            {
                start.ArgumentList.Add("--replay");
                start.ArgumentList.Add(replayOf);
            }
            else if (run.Options.Web is { } web)
            {
                start.ArgumentList.Add("--web");
                start.ArgumentList.Add(web.ToString().ToLowerInvariant());
            }
        }
        if (check.Kind == BenchmarkCheckKind.Script)
            foreach (string argument in check.Arguments) start.ArgumentList.Add(argument);
        start.Environment["PULS_API"] = pulsApi;
        // F31: the script (and the runner) tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find their run.
        start.Environment["BENCHMARK_RUN"] = run.Id;
        start.Environment["PYTHONUNBUFFERED"] = "1";      // so each line arrives as it is printed
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        return start;
    }

    /// <summary>Writes the stored run's export to the runner's stdin and closes it; a runner that ended without reading it is not an error here, its exit code says how it ended.</summary>
    private static void PipeInput(Process process, string input)
    {
        try
        {
            process.StandardInput.Write(input);
            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }
    }

    private void Launch(BenchmarkRun run, BenchmarkCheck check)
    {
        var start = StartInfoFor(check, run);

        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) run.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) run.Add(e.Data); };
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
            if (run.Input is { } input) PipeInput(process, input);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            run.Add(ex.Message);
            run.Complete(-1);
        }
    }
}
