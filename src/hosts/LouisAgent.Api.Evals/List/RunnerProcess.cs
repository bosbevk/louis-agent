using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Messages;
using System.Diagnostics;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Api.Evals.Messages;

namespace LouisAgent.Api.Evals.List;

/// <summary>
/// What one of the runner's commands gave: what it wrote on stdout, or why it gave nothing, in words.
/// </summary>
public sealed record RunnerOutput(string Stdout, string? Failure);

/// <summary>
/// Runs one of the eval runner's quick commands (<c>--list</c>, <c>--checks</c>) and hands back what it wrote, with a
/// timeout. A runner that isn't there, hangs or refuses gives the reason instead: its last line on stderr when it said
/// one.
/// </summary>
/// <remarks>
/// Extract Class (Fowler): the listing's process half, moved out of <c>EvalListing</c> so the studio's
/// <c>CheckListing</c> starts the runner the same way. The runner is started with an argument list, never through a
/// shell (ADR 0004).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public static class RunnerProcess
{
    /// <summary>
    /// Runs the command and reads its stdout; <paramref name="name"/> is what a failure calls it
    /// (<c>--list</c>), and <paramref name="requiredFile"/>, when given, must be there first.
    /// </summary>
    /// <remarks>Guard clauses: the file, then the start, the timeout and the exit code, each its own reason.</remarks>
    public static RunnerOutput Run(string command, IReadOnlyList<string> arguments, TimeSpan timeout,
        string? requiredFile, string name)
    {
        if (requiredFile is not null && !File.Exists(requiredFile))
            return Failed(EvalListingMessage.Missing, new { Path = requiredFile });

        var start = new ProcessStartInfo(command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);

        // A runner that can't start is the same warning as one that isn't there.
        try
        {
            using var process = Process.Start(start)!;
            // Both pipes are read while the process runs, so neither can fill and hang it.
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeout))
            {
                process.Kill(entireProcessTree: true);
                return Failed(EvalListingMessage.TimedOut, new
                {
                    Command = name,
                    Seconds = timeout.TotalSeconds
                });
            }

            return process.ExitCode == 0
                ? new RunnerOutput(output.Result, null)
                : Refused(name, process.ExitCode, errors.Result);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return Failed(EvalListingMessage.Missing, new { Path = command });
        }
    }

    /// <summary>
    /// The runner's refusal: its last line on stderr is the reason when there is one, else only the exit code.
    /// </summary>
    private static RunnerOutput Refused(string name, int code, string stderr)
    {
        string? reason = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
        return reason is null
            ? Failed(EvalListingMessage.ExitCode, new
            {
                Command = name,
                Code = code
            })
            : Failed(EvalListingMessage.ExitCodeWithReason, new
            {
                Command = name,
                Code = code,
                Reason = reason
            });
    }

    /// <summary>Nothing on stdout, and the reason in words a person reads.</summary>
    private static RunnerOutput Failed(EvalListingMessage message, object values) =>
        new("", MessageText.Format(message, values));
}
