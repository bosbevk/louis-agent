using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Runs a check's script as a child process (F27): the interpreter with an argument list, never a shell string, in the
/// workspace, with <c>PULS_API</c> pointing at this API and <c>BENCHMARK_RUN</c> its id; a run's results go to the
/// benchmark store through <c>BenchmarkRecorder</c> (F31). One run at a time: the containers are shared, and a second run would skew the first's
/// ledger and timings. Its lines (stdout and stderr) are kept on the run and streamed to listeners.
/// </summary>
/// <param name="interpreter">The Python to run; <c>python3</c> in the containers.</param>
public sealed class BenchmarkRunner(BenchmarkCatalog catalog, string workspaceRoot, string pulsApi, string interpreter = "python3",
    string evalRunner = LouisAgent.Api.Config.AdminOptions.DefaultEvalRunner)
{
    /// <summary>Repeats one run may ask for.</summary>
    public const int MaxRepeat = 5;

    private readonly Lock _lock = new();

    /// <summary>The run in progress, or the last one; null before the first.</summary>
    public BenchmarkRun? Current { get; private set; }

    public bool IsRunning => Current?.Status == BenchmarkRunStatus.Running;

    /// <summary>Starts a run; null when the check is unknown or one is already running.</summary>
    public BenchmarkRun? Start(string checkId, int repeat)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == checkId) is not { } check) return null;
        repeat = Math.Clamp(repeat, 1, MaxRepeat);

        lock (_lock)
        {
            if (IsRunning) return null;
            var run = new BenchmarkRun(Guid.NewGuid().ToString("N")[..12], check.Id, repeat);
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
    /// The start of one run's process (F18 step 4.3): an eval's <c>dotnet {EVAL_RUNNER} run {name} --repeat N</c>, or a script's
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
        start.ArgumentList.Add("--repeat");
        start.ArgumentList.Add(run.Repeat.ToString());
        if (check.Kind == BenchmarkCheckKind.Script)
            foreach (string argument in check.Arguments) start.ArgumentList.Add(argument);
        start.Environment["PULS_API"] = pulsApi;
        // F31: the script (and the runner) tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find their run.
        start.Environment["BENCHMARK_RUN"] = run.Id;
        start.Environment["PYTHONUNBUFFERED"] = "1";      // so each line arrives as it is printed
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        return start;
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
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            run.Add(ex.Message);
            run.Complete(-1);
        }
    }
}
