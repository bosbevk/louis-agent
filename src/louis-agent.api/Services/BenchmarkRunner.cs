using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LouisAgent.Api.Services;

/// <summary>
/// Runs a check's script as a child process (F27): the interpreter with an argument list, never a shell string, in the
/// workspace, with <c>PULS_API</c> pointing at this API; where a run's results go is the Benchmarks feature's (SQL
/// Server's <c>puls_benchmarks</c>; CL10 removed the run folders under <c>samples/</c>). One run at a time: the containers are shared, and a second run would skew the first's
/// ledger and timings. Its lines (stdout and stderr) are kept on the run and streamed to listeners.
/// </summary>
/// <param name="interpreter">The Python to run; <c>python3</c> in the containers.</param>
public sealed class BenchmarkRunner(BenchmarkCatalog catalog, string workspaceRoot, string pulsApi, string interpreter = "python3")
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

    private void Launch(BenchmarkRun run, BenchmarkCheck check)
    {
        var start = new ProcessStartInfo(interpreter)
        {
            WorkingDirectory = workspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        // Arguments as data (ADR 0004): the script's path and its arguments are a list, never a command string.
        start.ArgumentList.Add(check.Script);
        start.ArgumentList.Add("--repeat");
        start.ArgumentList.Add(run.Repeat.ToString());
        foreach (string argument in check.Arguments) start.ArgumentList.Add(argument);
        start.Environment["PULS_API"] = pulsApi;
        start.Environment["PYTHONUNBUFFERED"] = "1";      // so each line arrives as it is printed
        start.Environment["PYTHONIOENCODING"] = "utf-8";

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
