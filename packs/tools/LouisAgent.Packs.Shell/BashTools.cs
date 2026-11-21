
using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Composition.Tools.Scripts;
using System.Text;
using LouisAgent.Packs.Shell.Messages;

namespace LouisAgent.Packs.Shell;
/// <summary>
/// Bash toolset: run inline scripts or workspace .sh files with Git Bash on Windows or /bin/bash elsewhere.
/// Arguments become real positional parameters ($1, $2, ...) and scripts run in strict mode by default.
/// </summary>
public sealed class BashTools
{
    private readonly BashRunner _runner;

    /// <param name="workspaceRoot">Where scripts run.</param>
    public BashTools(string workspaceRoot) => _runner = new BashRunner(workspaceRoot);

    private const int MaxOutputChars = 12_000;

    private static readonly string[] CommonCommands =
        ["git", "curl", "jq", "grep", "find", "sed", "awk", "python3", "pwsh", "dotnet"];

    [Description("Runs a bash script, either inline via 'script' or an existing workspace-relative '.sh' file via " +
                 "'scriptPath' — pass exactly one of the two. On Windows this always runs Git Bash, never the WSL bash.exe in " +
                 "System32, which starts a separate Linux environment with its own filesystem; on Linux/macOS it runs /bin/bash. " +
                 "Script arguments arrive as $1, $2, ... and \"$@\", and the script runs under 'set -euo pipefail' by default so " +
                 "the first failing command stops it immediately (set strict to false to disable that). Output is capped at " +
                 "12,000 characters and the process is killed after timeoutSeconds (default 60, max 600). Use this for shell " +
                 "scripting, piping, and Unix-style text processing (grep/sed/awk/jq); for dotnet build/test/run use the " +
                 "dedicated DotNet tools instead, and for file reads/writes use the WorkspaceTools file tools rather than " +
                 "cat/echo through bash.")]
    public string BashRun(
        [Description("Inline bash script to run (leave empty when using scriptPath)")]
        string script = "",
        [Description("Workspace-relative path to a .sh file to run (leave empty when using script)")]
        string scriptPath = "",
        [Description(
            "Arguments passed as $1, $2, ...; space separated, wrap an argument in double quotes to keep spaces")]
        string arguments = "",
        [Description("Text sent to standard input (stdin is closed afterwards)")]
        string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")]
        int timeoutSeconds = 60,
        [Description("Run with 'set -euo pipefail' so the first failing command stops the script (default true)")]
        bool strict = true)
    {
        bool hasScript = !string.IsNullOrWhiteSpace(script);
        bool hasPath = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasScript == hasPath) return MessageText.Format(ShellToolMessage.PassScriptOrScriptPath);

        string? file = null;
        if (hasPath)
        {
            if (!TryResolveWorkspacePath(scriptPath, out file, out var error)) return error;
            if (!File.Exists(file))
                return MessageText.Format(ShellToolMessage.ScriptNotFound, new { Path = scriptPath });
        }

        var result = _runner.RunScript(hasScript ? script : null, file, ProcessRunner.SplitArguments(arguments), standardInput,
            TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), strict, out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? MessageText.Format(ShellToolMessage.ScriptKilled, new { Seconds = timeoutSeconds })
            : MessageText.Format(ToolMessage.ProcessExitCode, new { result.ExitCode }));
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        // Linux bash treats the \r of Windows line endings as part of each command ("$'\r': command not found",
        // "syntax error near unexpected token"); Git Bash tolerates it, so only hint when the script failed.
        if (file is not null && result.ExitCode != 0 && File.ReadAllText(file).Contains('\r'))
            sb.Append(MessageText.Format(ShellToolMessage.BashCrlfHint));
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    [Description(
        "Reports which bash executable BashRun will use — Git Bash on Windows (found via BASH_PATH/SKILL_SHELL, " +
        "the Program Files install, or next to git.exe on PATH) or /bin/bash on Linux/macOS — along with its version " +
        "string and whether common command-line tools (git, curl, jq, grep, find, sed, awk, python3, pwsh, dotnet) are " +
        "present on PATH. Use this to diagnose a BashRun failure caused by a missing interpreter or missing tool before " +
        "retrying the same script. It makes no changes and has no side effects; it runs only a fixed version/tool-" +
        "presence probe, never arbitrary commands.")]
    public string BashInfo()
    {
        if (BashRunner.FindBash() is not { } bash) return BashRunner.NotInstalledMessage;

        string probe =
            $"echo \"Version: $BASH_VERSION\"; echo Commands:; for c in {string.Join(' ', CommonCommands)}; do " +
            "if command -v \"$c\" >/dev/null 2>&1; then echo \"  $c: available\"; else echo \"  $c: missing\"; fi; done";
        var result = ProcessRunner.Run(bash, ["-c", probe], _runner.WorkingDirectory, TimeSpan.FromSeconds(20));
        return $"Executable: {bash}\n{result.Output.Trim()}";
    }

    internal bool TryResolveWorkspacePath(string relativePath, out string fullPath, out string error)
    {
        string root = Path.GetFullPath(_runner.WorkingDirectory);
        fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        error = "";

        string rootWithSeparator =
            root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return true;

        error = MessageText.Format(ToolMessage.PathOutsideWorkspaceNamed, new { Path = relativePath });
        return false;
    }
}
