using System.Diagnostics;
using System.Text;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// Runs git for an endpoint, with its arguments as a list and never through a shell, and answers a failed run in the
/// API's error shape (<c>git_error</c>).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
internal static class GitProcess
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>A failed git run as the API's error: what git said, or its exit code when it said nothing.</summary>
    internal static JsonHttpResult<ApiError> GitError(GitResult result) =>
        ApiErrors.GitFailed(string.IsNullOrWhiteSpace(result.Error)
            ? MessageText.Format(ApiUserText.GitExited, new { result.ExitCode })
            : result.Error.Trim());

    /// <summary>
    /// Runs git in the workspace and hands back its exit code and what it printed; a run that takes too long is
    /// stopped and says so.
    /// </summary>
    internal static async Task<GitResult> RunAsync(AgentEngine engine, IEnumerable<string> args)
    {
        // stdout and stderr are kept apart: git prints warnings (e.g. about line endings) on stderr that would corrupt
        // the porcelain and diff output.
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
            // Too slow: stop git and everything it started, and say how long it was given.
            process.Kill(entireProcessTree: true);
            return new GitResult(-1, "",
                MessageText.Format(ApiUserText.GitTimedOut, new { Seconds = (int)GitTimeout.TotalSeconds }));
        }

        return new GitResult(process.ExitCode, await output, await error);
    }

    /// <summary>How a git run ended: its exit code, and what it printed on each stream.</summary>
    internal sealed record GitResult(int ExitCode, string Output, string Error);
}
