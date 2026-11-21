using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools.Scripts.Messages;
using System.Text;

namespace LouisAgent.Composition.Tools.Scripts;

/// <summary>
/// Runs PowerShell for whoever needs it: the powershell tools, and the engine's own skills and agent-built tools.
/// It starts PowerShell 7 or, on Windows, 5.1, without profiles or prompts and with UTF-8 output.
/// </summary>
/// <remarks>
/// Shared here because the engine runs skills whether or not a session has the toolset, and a pack can't be referenced
/// by the engine. What the model calls stays in the pack; only running a script is common.
/// </remarks>
public sealed class PowerShellRunner
{
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

    /// <summary>A runner over this workspace.</summary>
    public PowerShellRunner(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    /// <summary>
    /// Runs inline script text (written to a temp .ps1) or an existing .ps1 file through the UTF-8 wrapper.
    /// Shared with ExecuteSkill and agent-built tools. Returns null with <paramref name="notInstalled"/> set when no PowerShell is available.
    /// </summary>
    public ProcessRunner.Result? RunScript(
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

    /// <summary>A PowerShell to start, and its fixed arguments.</summary>
    public sealed record Interpreter(string FileName, string[] BaseArgs);

    /// <summary>What to tell the model when there's no PowerShell.</summary>
    public static string NotInstalledMessage => MessageText.Format(RunnerToolMessage.PowerShellNotInstalled);

    /// <summary>Where scripts run.</summary>
    public string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

    public Interpreter? ResolveInterpreter() => _interpreter ??= FindInterpreter();

    private Interpreter? FindInterpreter()
    {
        var candidates = new List<string>();
        if (Environment.GetEnvironmentVariable("POWERSHELL_PATH") is { Length: > 0 } configured)
            candidates.Add(configured);
        candidates.Add("pwsh");
        if (OperatingSystem.IsWindows()) candidates.Add("powershell.exe");

        foreach (string candidate in candidates)
        {
            // ExecutionPolicy only exists on Windows; elsewhere scripts are always allowed.
            string[] baseArgs = OperatingSystem.IsWindows()
                ? ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass"]
                : ["-NoProfile", "-NonInteractive"];
            var probe = ProcessRunner.Run(candidate, [.. baseArgs, "-Command", "$PSVersionTable.PSVersion.Major"],
                WorkingDirectory, ProbeTimeout);
            if (probe.ExitCode == 0 && int.TryParse(probe.Output.Trim(), out _))
                return new Interpreter(candidate, baseArgs);
        }

        return null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
