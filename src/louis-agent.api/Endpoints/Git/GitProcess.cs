using System.Diagnostics;
using System.Text;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// Runs git for an endpoint, with its arguments as a list and never through a shell (ADR 0004), and answers a failed run in the
/// API's error shape (<c>git_error</c>).
/// </summary>
internal static class GitProcess
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    internal static IResult GitError(GitResult result) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "git_error",
            string.IsNullOrWhiteSpace(result.Error) ? MessageText.Format(ApiUserText.GitExited, new { result.ExitCode }) : result.Error.Trim());

    // stdout and stderr are kept apart: git prints warnings (e.g. about line endings) on stderr that would corrupt
    // the porcelain and diff output.
    internal static async Task<GitResult> RunAsync(AgentEngine engine, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = engine.WorkspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // Report non-ASCII paths as-is instead of octal-escaped.
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("core.quotepath=false");
        foreach (string arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(GitTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return new GitResult(-1, "", MessageText.Format(ApiUserText.GitTimedOut, new { Seconds = (int)GitTimeout.TotalSeconds }));
        }

        return new GitResult(process.ExitCode, await output, await error);
    }

    internal sealed record GitResult(int ExitCode, string Output, string Error);
}
