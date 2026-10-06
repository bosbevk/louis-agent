namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Text;

/// <summary>
/// PowerShell toolset: run inline scripts or workspace .ps1 files with PowerShell 7 (pwsh) or, on Windows,
/// Windows PowerShell 5.1. Runs without profiles or prompts, with UTF-8 output and a timeout.
/// </summary>
public sealed class PowerShellTools
{
    private const int MaxOutputChars = 12_000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);

    // UTF-8 with BOM: Windows PowerShell 5.1 reads BOM-less scripts as ANSI and mangles non-ASCII text.
    private static readonly UTF8Encoding ScriptEncoding = new(encoderShouldEmitUTF8Identifier: true);

    // Runs the target script with UTF-8 output (5.1 otherwise writes redirected output in the OEM code page)
    // and propagates its exit code: an explicit 'exit n' wins, an uncaught error gives 1.
    private const string WrapperScript = """
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $target = $args[0]
        $rest = @($args | Select-Object -Skip 1)
        $global:LASTEXITCODE = 0
        try { & $target @rest } catch { [Console]::Error.WriteLine(($_ | Out-String).Trim()); exit 1 }
        exit $LASTEXITCODE
        """;

    private readonly string _workspaceRoot;
    private Interpreter? _interpreter;

    public PowerShellTools(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    [Description("Run PowerShell. Pass either an inline 'script' or a workspace-relative 'scriptPath' (.ps1). Returns output, exit code and errors. Uses pwsh (PowerShell 7) when installed, else Windows PowerShell 5.1.")]
    public string PowerShellRun(
        [Description("Inline PowerShell script to run (leave empty when using scriptPath)")] string script = "",
        [Description("Workspace-relative path to a .ps1 file to run (leave empty when using script)")] string scriptPath = "",
        [Description("Arguments for the script ($args / param block), space separated; wrap an argument in double quotes to keep spaces")] string arguments = "",
        [Description("Text sent to standard input; read it with [Console]::In.ReadToEnd()")] string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")] int timeoutSeconds = 60)
    {
        bool hasScript = !string.IsNullOrWhiteSpace(script);
        bool hasPath = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasScript == hasPath) return "Error: pass exactly one of 'script' or 'scriptPath'.";

        string target;
        if (hasPath)
        {
            if (!TryResolveWorkspacePath(scriptPath, out target, out var error)) return error;
            if (!File.Exists(target)) return $"Error: script '{scriptPath}' not found.";
            if (!target.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)) return "Error: scriptPath must be a .ps1 file.";
        }
        else target = "";

        var result = RunScript(hasScript ? script : null, hasPath ? target : null, DotNetTools.SplitArguments(arguments),
            standardInput, TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? $"Script was still running after {timeoutSeconds}s and was killed."
            : $"Exit code: {result.ExitCode}");
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    [Description("Show which PowerShell is used (pwsh 7 or Windows PowerShell 5.1), its version, edition and OS.")]
    public string PowerShellInfo()
    {
        var result = RunScript("""
            "Edition: $($PSVersionTable.PSEdition)"
            "Version: $($PSVersionTable.PSVersion)"
            "OS: $([System.Environment]::OSVersion.VersionString)"
            """, null, [], null, ProbeTimeout, out string? notInstalled);
        if (notInstalled is not null) return notInstalled;
        return $"Executable: {_interpreter!.FileName}\n{result!.Output.Trim()}";
    }

    /// <summary>
    /// Runs inline script text (written to a temp .ps1) or an existing .ps1 file through the UTF-8 wrapper.
    /// Shared with ExecuteSkill and agent-built tools. Returns null with <paramref name="notInstalled"/> set when no PowerShell is available.
    /// </summary>
    internal ProcessRunner.Result? RunScript(
        string? inlineScript,
        string? scriptFile,
        IReadOnlyList<string> arguments,
        string? standardInput,
        TimeSpan timeout,
        out string? notInstalled,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        notInstalled = null;
        if (ResolveInterpreter() is not { } shell)
        {
            notInstalled = NotInstalledMessage;
            return null;
        }

        string tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "louis-agent-powershell")).FullName;
        string id = Guid.NewGuid().ToString("N");
        string wrapper = Path.Combine(tempDir, $"wrapper_{id}.ps1");
        string? tempScript = inlineScript is null ? null : Path.Combine(tempDir, $"script_{id}.ps1");

        try
        {
            File.WriteAllText(wrapper, WrapperScript, ScriptEncoding);
            if (tempScript is not null) File.WriteAllText(tempScript, inlineScript, ScriptEncoding);

            var result = ProcessRunner.Run(shell.FileName,
                [.. shell.BaseArgs, "-File", wrapper, tempScript ?? scriptFile!, .. arguments],
                WorkingDirectory, timeout, standardInput, environment);

            // Show a stable name instead of the temp path in error positions.
            return tempScript is null ? result : result with { Output = result.Output.Replace(tempScript, "<script>") };
        }
        finally
        {
            TryDelete(wrapper);
            if (tempScript is not null) TryDelete(tempScript);
        }
    }

    internal sealed record Interpreter(string FileName, string[] BaseArgs);

    private const string NotInstalledMessage =
        "PowerShell is not available. Install PowerShell 7 (https://aka.ms/powershell or 'winget install Microsoft.PowerShell'), " +
        "or set POWERSHELL_PATH to pwsh/powershell.exe.";

    private string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

    internal Interpreter? ResolveInterpreter() => _interpreter ??= FindInterpreter();

    private Interpreter? FindInterpreter()
    {
        var candidates = new List<string>();
        if (Environment.GetEnvironmentVariable("POWERSHELL_PATH") is { Length: > 0 } configured) candidates.Add(configured);
        candidates.Add("pwsh");
        if (OperatingSystem.IsWindows()) candidates.Add("powershell.exe");

        foreach (string candidate in candidates)
        {
            // ExecutionPolicy only exists on Windows; elsewhere scripts are always allowed.
            string[] baseArgs = OperatingSystem.IsWindows()
                ? ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass"]
                : ["-NoProfile", "-NonInteractive"];
            var probe = ProcessRunner.Run(candidate, [.. baseArgs, "-Command", "$PSVersionTable.PSVersion.Major"], WorkingDirectory, ProbeTimeout);
            if (probe.ExitCode == 0 && int.TryParse(probe.Output.Trim(), out _)) return new Interpreter(candidate, baseArgs);
        }

        return null;
    }

    internal bool TryResolveWorkspacePath(string relativePath, out string fullPath, out string error)
    {
        string root = Path.GetFullPath(WorkingDirectory);
        fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        error = "";

        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return true;

        error = $"Error: '{relativePath}' is outside the workspace.";
        return false;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
