
using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Composition.Tools.Scripts;
using System.Text;
using LouisAgent.Packs.Shell.Messages;

namespace LouisAgent.Packs.Shell;
/// <summary>
/// PowerShell toolset: run inline scripts or workspace .ps1 files with PowerShell 7 (pwsh) or, on Windows,
/// Windows PowerShell 5.1. Runs without profiles or prompts, with UTF-8 output and a timeout.
/// </summary>
public sealed class PowerShellTools
{
    private readonly PowerShellRunner _runner;

    /// <param name="workspaceRoot">Where scripts run.</param>
    public PowerShellTools(string workspaceRoot) => _runner = new PowerShellRunner(workspaceRoot);

    private const int MaxOutputChars = 12_000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);

    [Description(
        "Runs a PowerShell script, either inline via 'script' or an existing workspace-relative '.ps1' file via " +
        "'scriptPath' — pass exactly one of the two. Prefers PowerShell 7 (pwsh) when installed and falls back to " +
        "Windows PowerShell 5.1 on Windows; it always runs without a profile, without prompts, and with UTF-8 console " +
        "output so non-ASCII text round-trips correctly on 5.1. An uncaught error inside the script sets exit code 1 " +
        "and its message is captured; an explicit 'exit n' is honored as-is. Output is capped at 12,000 characters and " +
        "the process is killed after timeoutSeconds (default 60, max 600). Use this for Windows-specific automation, " +
        ".NET object pipelines, or PowerShell cmdlets; for portable shell scripting or Unix text tools use BashRun " +
        "instead.")]
    public string PowerShellRun(
        [Description("Inline PowerShell script to run (leave empty when using scriptPath)")]
        string script = "",
        [Description("Workspace-relative path to a .ps1 file to run (leave empty when using script)")]
        string scriptPath = "",
        [Description(
            "Arguments for the script ($args / param block), space separated; wrap an argument in double quotes to keep spaces")]
        string arguments = "",
        [Description("Text sent to standard input; read it with [Console]::In.ReadToEnd()")]
        string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")]
        int timeoutSeconds = 60)
    {
        bool hasScript = !string.IsNullOrWhiteSpace(script);
        bool hasPath = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasScript == hasPath) return MessageText.Format(ShellToolMessage.PassScriptOrScriptPath);

        string target;
        if (hasPath)
        {
            if (!TryResolveWorkspacePath(scriptPath, out target, out var error)) return error;
            if (!File.Exists(target))
                return MessageText.Format(ShellToolMessage.ScriptNotFound, new { Path = scriptPath });
            if (!target.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return MessageText.Format(ShellToolMessage.ScriptPathNotPs1);
        }
        else target = "";

        var result = _runner.RunScript(hasScript ? script : null, hasPath ? target : null,
            ProcessRunner.SplitArguments(arguments),
            standardInput, TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? MessageText.Format(ShellToolMessage.ScriptKilled, new { Seconds = timeoutSeconds })
            : MessageText.Format(ToolMessage.ProcessExitCode, new { result.ExitCode }));
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    [Description("Reports which PowerShell interpreter PowerShellRun will use — pwsh (PowerShell 7, preferred) or " +
                 "Windows PowerShell 5.1 — along with its edition, version, and operating system. Use this to diagnose a " +
                 "PowerShellRun failure caused by a missing interpreter before retrying the same script, or to confirm which " +
                 "PowerShell edition's cmdlets are actually available. It makes no changes and runs only a fixed version probe, " +
                 "not arbitrary commands.")]
    public string PowerShellInfo()
    {
        var result = _runner.RunScript("""
                               "Edition: $($PSVersionTable.PSEdition)"
                               "Version: $($PSVersionTable.PSVersion)"
                               "OS: $([System.Environment]::OSVersion.VersionString)"
                               """, null, [], null, ProbeTimeout, out string? notInstalled);
        if (notInstalled is not null) return notInstalled;
        return $"Executable: {_runner.ResolveInterpreter()!.FileName}\n{result!.Output.Trim()}";
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
