
using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Composition.Tools.Scripts;
using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Packs.Shell.Messages;

namespace LouisAgent.Packs.Shell;
/// <summary>
/// Python toolset: run inline code or workspace scripts and manage packages in a dedicated virtual environment,
/// so installs never touch the system Python or the repository.
/// </summary>
public sealed partial class PythonTools
{
    private readonly PythonRunner _runner;

    /// <param name="workspaceRoot">Where scripts run.</param>
    /// <param name="venvDirectory">Virtual environment for installed packages; defaults to PYTHON_VENV or a per-user folder.</param>
    public PythonTools(string workspaceRoot, string? venvDirectory = null) =>
        _runner = new PythonRunner(workspaceRoot, venvDirectory);

    private const int MaxOutputChars = 12_000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);

    [Description("Runs Python code, either inline via 'code' or an existing workspace-relative '.py' file via " +
                 "'scriptPath' — pass exactly one of the two. Uses the agent's own virtual environment once " +
                 "PythonInstallPackages has created one (so installed packages are isolated from the system Python and never " +
                 "touch the repository), otherwise the first Python 3 interpreter found on the machine; it never uses a " +
                 "Windows Store Python stub. Output is capped at 12,000 characters, and the process is killed after " +
                 "timeoutSeconds (default 60, max 600) with its exit code and any traceback included in the result. Use this " +
                 "for data processing, calculations, or anything needing a Python package; for simple file reads/writes " +
                 "prefer the WorkspaceTools file tools, and for shell commands prefer BashRun.")]
    public string PythonRun(
        [Description("Inline Python source to run (leave empty when using scriptPath)")]
        string code = "",
        [Description("Workspace-relative path to a .py file to run (leave empty when using code)")]
        string scriptPath = "",
        [Description(
            "Arguments for the script (sys.argv[1:]), space separated; wrap an argument in double quotes to keep spaces")]
        string arguments = "",
        [Description("Text sent to the program's standard input (stdin is closed afterwards)")]
        string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")]
        int timeoutSeconds = 60)
    {
        bool hasCode = !string.IsNullOrWhiteSpace(code);
        bool hasScript = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasCode == hasScript) return MessageText.Format(ShellToolMessage.PassCodeOrScriptPath);

        string? script = null;
        if (hasScript)
        {
            if (!TryResolveWorkspacePath(scriptPath, out script, out var error)) return error;
            if (!File.Exists(script))
                return MessageText.Format(ShellToolMessage.ScriptNotFound, new { Path = scriptPath });
        }

        var result = _runner.RunScript(hasCode ? code : null, script, ProcessRunner.SplitArguments(arguments), standardInput,
            TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? MessageText.Format(ToolMessage.ProgramKilled, new { Seconds = timeoutSeconds })
            : MessageText.Format(ToolMessage.ProcessExitCode, new { result.ExitCode }));
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    [Description(
        "Installs one or more packages with pip into the agent's dedicated virtual environment, creating that " +
        "environment on first use so installs never affect the system Python or any other project. Takes " +
        "space-separated pip requirement specifiers only (e.g. 'requests' or 'numpy>=2 pandas==2.2.2') — pip options " +
        "like --index-url or -r requirements.txt are rejected, and each specifier is validated against a strict " +
        "pattern before anything runs. Installation can take up to 5 minutes for large packages; call PythonInfo " +
        "afterward to confirm what actually got installed. Use this before PythonRun needs a package that isn't " +
        "already present — check PythonInfo first if unsure what's installed.")]
    public string PythonInstallPackages(
        [Description("Space-separated pip requirement specifiers, e.g. 'requests' or 'numpy>=2 pandas==2.2.2'")]
        string packages)
    {
        var specs = (packages ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (specs.Length == 0) return MessageText.Format(ShellToolMessage.PackagesRequired);
        // Requirement specifiers only — no pip options such as --index-url or -r.
        if (specs.FirstOrDefault(s => !RequirementSpec().IsMatch(s)) is { } invalid)
            return MessageText.Format(ShellToolMessage.PackageSpecifierInvalid, new { Specifier = invalid });

        if (EnsureVirtualEnvironment() is { } venvError) return venvError;

        var result = ProcessRunner.Run(_runner.VenvPython, ["-m", "pip", "install", "--disable-pip-version-check", .. specs],
            _runner.WorkingDirectory, InstallTimeout, environment: PythonRunner.PythonEnvironment);
        if (result.TimedOut)
            return MessageText.Format(ShellToolMessage.PipTimedOut, new { Output = ProcessRunner.Tail(result.Output) });

        // pip is chatty; the final lines say what was installed or why it failed.
        return result.ExitCode == 0
            ? ProcessRunner.Tail(result.Output, 1500)
            : MessageText.Format(ShellToolMessage.PipFailed,
                new { result.ExitCode, Output = ProcessRunner.Tail(result.Output) });
    }

    [Description("Reports which Python interpreter PythonRun will use, its version, the virtual environment's " +
                 "location, and the full list of packages currently installed in it (via pip list). If the virtual " +
                 "environment hasn't been created yet, says so rather than listing packages. Use this to check whether a " +
                 "package is already available before calling PythonInstallPackages, or to diagnose a PythonRun failure caused " +
                 "by a missing interpreter or package. It makes no changes and runs only version/package-listing probes, not " +
                 "arbitrary commands.")]
    public string PythonInfo()
    {
        if (_runner.ResolveInterpreter() is not { } python) return PythonRunner.NotInstalledMessage;

        var version = ProcessRunner.Run(python.FileName, [.. python.PrefixArgs, "--version"], _runner.WorkingDirectory,
            ProbeTimeout);
        var sb = new StringBuilder();
        sb.AppendLine($"Interpreter: {python.FileName} {string.Join(' ', python.PrefixArgs)}".TrimEnd());
        sb.AppendLine($"Version: {version.Output.Trim()}");
        sb.AppendLine(
            $"Virtual environment: {_runner.VenvDirectory} ({(File.Exists(_runner.VenvPython) ? MessageText.Format(ShellToolMessage.VenvCreated) : MessageText.Format(ShellToolMessage.VenvNotCreatedYet))})");

        if (File.Exists(_runner.VenvPython))
        {
            var packages = ProcessRunner.Run(_runner.VenvPython,
                ["-m", "pip", "list", "--format=freeze", "--disable-pip-version-check"],
                _runner.WorkingDirectory, TimeSpan.FromMinutes(1), environment: PythonRunner.PythonEnvironment);
            sb.AppendLine("Installed packages:");
            sb.Append(string.IsNullOrWhiteSpace(packages.Output) ? "(none)" : packages.Output.Trim());
        }

        return ProcessRunner.Truncate(sb.ToString().TrimEnd(), MaxOutputChars);
    }

    private string? EnsureVirtualEnvironment()
    {
        if (File.Exists(_runner.VenvPython)) return null;
        if (_runner.ResolveInterpreter() is not { } python) return PythonRunner.NotInstalledMessage;

        Directory.CreateDirectory(Path.GetDirectoryName(_runner.VenvDirectory)!);
        var result = ProcessRunner.Run(python.FileName, [.. python.PrefixArgs, "-m", "venv", _runner.VenvDirectory],
            _runner.WorkingDirectory, InstallTimeout, environment: PythonRunner.PythonEnvironment);
        if (result.ExitCode != 0 || !File.Exists(_runner.VenvPython))
            return MessageText.Format(ShellToolMessage.VenvNotCreated,
                new { Directory = _runner.VenvDirectory, Output = ProcessRunner.Tail(result.Output) });

        _runner.ForgetBaseInterpreter();
        return null;
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

    // name[extras] followed by optional version constraints, e.g. requests, uvicorn[standard], numpy>=2,<3
    [GeneratedRegex(
        @"^[A-Za-z0-9][A-Za-z0-9._-]*(\[[A-Za-z0-9._,-]+\])?([<>=!~]=?[A-Za-z0-9.*+!_-]+(,[<>=!~]=?[A-Za-z0-9.*+!_-]+)*)?$")]
    internal static partial Regex RequirementSpec();
}
