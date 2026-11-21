using System.ComponentModel;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Orchestrator.Tools.Checks;

/// <summary>
/// The orchestrator's checks of a fix the coding agent claims: the commit is really on its branch, the failing
/// request now answers, the service's tests pass. A claim is not proof; these run against the repository and the
/// service itself. Every public method is a tool.
/// </summary>
/// <remarks>
/// Split out of <see cref="ServiceTools"/> by responsibility (ADR 0034, F106.3.1): that class calls the coder and
/// records the decisions, this one checks the work. The tools and their descriptions stay exactly as they were, so
/// the model's view doesn't change with the split.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
internal sealed class FixChecks(OrchestratorOptions options, IReadOnlySet<string> methods, CommsLog comms)
{
    private static readonly ILogger Log = AgentLogging.For<FixChecks>();
    private static readonly TimeSpan DotNetTimeout = TimeSpan.FromMinutes(5);

    private readonly ServiceRepository _repository = new(options);

    /// <summary>Checks a commit louis-agent says it made, in the service's own repository.</summary>
    [Description(
        "Checks a commit louis-agent says it made, directly in the service's git repository: the commit exists, " +
        "it is on the reported branch, that branch is checked out and starts from the tip of main, the commit is NOT on " +
        "main (fixes go through review), it changed files, and the working tree is clean. Returns each check as " +
        "PASS/FAIL plus the commit's author, message and changed files, ending with VERIFIED or NOT VERIFIED.")]
    public string VerifyFixCommit(
        [Description("The commit hash louis-agent reported (7-40 hex characters).")]
        string commit,
        [Description("The branch louis-agent reported, e.g. fix/order-total-abc123.")]
        string branch)
    {
        if (!ServiceRepository.IsCommitHash(commit))
            return MessageText.Format(OrchestratorToolMessage.InvalidCommitHash);
        if (!ServiceRepository.IsBranchName(branch))
            return MessageText.Format(OrchestratorToolMessage.InvalidBranchName);

        Info(OrchestratorLogMessage.VerifyingCommit, new
        {
            Commit = commit,
            Branch = branch
        });
        var (report, ok) = _repository.Verify(commit, branch);
        foreach (string line in report.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            Info(OrchestratorLogMessage.Result, new { Line = line.TrimEnd() });
        comms.Check($"VerifyFixCommit({commit}, {branch})", ok ? "**VERIFIED**" : "**NOT VERIFIED**", report);
        return report;
    }

    /// <summary>Calls a service method again on the code that's checked out, to see whether it still fails.</summary>
    [Description(
        "Calls one of the service's API methods again with the same arguments, against the code currently checked " +
        "out (after a fix: the fix branch), to see whether the exception still happens. Replays never reach the " +
        "production error log. Returns the exit code and the service's response: a line starting with 200 is " +
        "success, 500 means it still fails.")]
    public string ReplayRequest(
        [Description("The API method, as in the error event (e.g. order-total).")]
        string method,
        [Description("The order id argument from the error event.")]
        int orderId)
    {
        if (!methods.Contains(method))
            return MessageText.Format(OrchestratorToolMessage.UnknownMethod,
                new
                {
                    Method = method,
                    Known = string.Join(", ", methods)
                });

        // Replays log their errors to a file of their own, so they never reach the production error log.
        Info(OrchestratorLogMessage.Replaying, new
        {
            Method = method,
            OrderId = orderId
        });
        Directory.CreateDirectory(options.StateDirectory);
        var replayLog = Path.Combine(options.StateDirectory, "replay-errors.jsonl");
        var run = ProcessRunner.Run("dotnet",
            ["run", "--project", options.ServiceProject, "--", method, orderId.ToString()],
            options.ServiceRoot, DotNetTimeout,
            environment: new Dictionary<string, string> { ["ERROR_LOG_PATH"] = replayLog });
        string response = LastLine(run.Output, "200 ", "500 ") ?? ProcessRunner.Tail(run.Output, 2000);
        Info(OrchestratorLogMessage.Result, new { Line = response });
        comms.Check($"ReplayRequest({method} {orderId})", $"`{response.ReplaceLineEndings(" ")}`");
        return ExitCodeAnd(run, response);
    }

    /// <summary>Runs the service's whole test suite on the code that's checked out.</summary>
    [Description("Runs the service's whole test suite (dotnet test in the service repository) on the code currently " +
                 "checked out. Returns the exit code and the end of the output with the pass/fail summary.")]
    public string RunServiceTests()
    {
        Info(OrchestratorLogMessage.RunningTests);
        var run = ProcessRunner.Run("dotnet", ["test"], options.ServiceRoot, DotNetTimeout);
        string summary = LastLine(run.Output, "Passed!", "Failed!") ??
                         MessageText.Format(OrchestratorToolMessage.ExitCode, new { run.ExitCode });
        Info(OrchestratorLogMessage.Result, new { Line = summary });
        comms.Check("RunServiceTests", $"`{summary}`");
        return ExitCodeAnd(run, ProcessRunner.Tail(run.Output, 2500));
    }

    /// <summary>A process's result for the model: its exit code, whether it timed out, then its output.</summary>
    private static string ExitCodeAnd(ProcessRunner.Result run, string output) =>
        MessageText.Format(OrchestratorToolMessage.ExitCodeAndOutput, new
        {
            run.ExitCode,
            TimedOut = run.TimedOut ? MessageText.Format(OrchestratorToolMessage.TimedOutSuffix) : "",
            Output = output,
        });

    /// <summary>Logs a step the orchestrator took, tagged so it reads apart from louis-agent's lines.</summary>
    private static void Info(OrchestratorLogMessage message, object? values = null) =>
        Log.Log(LogLevel.Information, message, values, tag: "orchestrator");

    /// <summary>The last line of a process's output that starts with one of the prefixes, such as "Passed!".</summary>
    private static string? LastLine(string output, params string[] prefixes) =>
        output.Split('\n').Select(l => l.Trim())
            .LastOrDefault(l => prefixes.Any(p => l.StartsWith(p, StringComparison.Ordinal)));
}
