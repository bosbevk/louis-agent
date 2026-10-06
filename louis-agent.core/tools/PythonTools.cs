namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Python toolset: run inline code or workspace scripts and manage packages in a dedicated virtual environment,
/// so installs never touch the system Python or the repository.
/// </summary>
public sealed partial class PythonTools
{
    private const int MaxOutputChars = 12_000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<string, string> PythonEnvironment = new()
    {
        ["PYTHONIOENCODING"] = "utf-8",
        ["PYTHONUNBUFFERED"] = "1",
        ["PYTHONDONTWRITEBYTECODE"] = "1",
        ["PIP_DISABLE_PIP_VERSION_CHECK"] = "1",
    };

    private readonly string _workspaceRoot;
    private readonly string _venvDirectory;
    private Interpreter? _baseInterpreter;

    /// <param name="venvDirectory">Virtual environment for installed packages; defaults to PYTHON_VENV or a per-user folder.</param>
    public PythonTools(string workspaceRoot, string? venvDirectory = null)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _venvDirectory = Path.GetFullPath(venvDirectory ?? DefaultVenvDirectory());
    }

    private static string DefaultVenvDirectory() =>
        Environment.GetEnvironmentVariable("PYTHON_VENV") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "louis-agent", "python-venv");

    [Description("Runs Python code, either inline via 'code' or an existing workspace-relative '.py' file via " +
        "'scriptPath' — pass exactly one of the two. Uses the agent's own virtual environment once " +
        "PythonInstallPackages has created one (so installed packages are isolated from the system Python and never " +
        "touch the repository), otherwise the first Python 3 interpreter found on the machine; it never uses a " +
        "Windows Store Python stub. Output is capped at 12,000 characters, and the process is killed after " +
        "timeoutSeconds (default 60, max 600) with its exit code and any traceback included in the result. Use this " +
        "for data processing, calculations, or anything needing a Python package; for simple file reads/writes " +
        "prefer the WorkspaceTools file tools, and for shell commands prefer BashRun.")]
    public string PythonRun(
        [Description("Inline Python source to run (leave empty when using scriptPath)")] string code = "",
        [Description("Workspace-relative path to a .py file to run (leave empty when using code)")] string scriptPath = "",
        [Description("Arguments for the script (sys.argv[1:]), space separated; wrap an argument in double quotes to keep spaces")] string arguments = "",
        [Description("Text sent to the program's standard input (stdin is closed afterwards)")] string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")] int timeoutSeconds = 60)
    {
        bool hasCode = !string.IsNullOrWhiteSpace(code);
        bool hasScript = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasCode == hasScript) return "Error: pass exactly one of 'code' or 'scriptPath'.";

        string? script = null;
        if (hasScript)
        {
            if (!TryResolveWorkspacePath(scriptPath, out script, out var error)) return error;
            if (!File.Exists(script)) return $"Error: script '{scriptPath}' not found.";
        }

        var result = RunScript(hasCode ? code : null, script, DotNetTools.SplitArguments(arguments), standardInput,
            TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? $"Program was still running after {timeoutSeconds}s and was killed."
            : $"Exit code: {result.ExitCode}");
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    /// <summary>
    /// Runs inline code (written to a temp .py) or an existing script file. Shared with ExecuteSkill and agent-built tools.
    /// Returns null with <paramref name="notInstalled"/> set when no Python 3 is available.
    /// </summary>
    internal ProcessRunner.Result? RunScript(
        string? inlineCode,
        string? scriptFile,
        IReadOnlyList<string> arguments,
        string? standardInput,
        TimeSpan timeout,
        out string? notInstalled,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        notInstalled = null;
        if (ResolveInterpreter() is not { } python)
        {
            notInstalled = NotInstalledMessage;
            return null;
        }

        string? tempFile = null;
        if (inlineCode is not null)
        {
            string tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "louis-agent-python")).FullName;
            tempFile = Path.Combine(tempDir, $"snippet_{Guid.NewGuid():N}.py");
            File.WriteAllText(tempFile, inlineCode, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        var env = new Dictionary<string, string>(PythonEnvironment);
        if (environment is not null)
            foreach (var (key, value) in environment) env[key] = value;

        try
        {
            var result = ProcessRunner.Run(python.FileName, [.. python.PrefixArgs, tempFile ?? scriptFile!, .. arguments],
                WorkingDirectory, timeout, standardInput, env);
            // Inline snippets run from a temp file; show a stable name in tracebacks instead.
            return tempFile is null ? result : result with { Output = result.Output.Replace(tempFile, "<snippet>") };
        }
        finally
        {
            if (tempFile is not null) TryDelete(tempFile);
        }
    }

    [Description("Installs one or more packages with pip into the agent's dedicated virtual environment, creating that " +
        "environment on first use so installs never affect the system Python or any other project. Takes " +
        "space-separated pip requirement specifiers only (e.g. 'requests' or 'numpy>=2 pandas==2.2.2') — pip options " +
        "like --index-url or -r requirements.txt are rejected, and each specifier is validated against a strict " +
        "pattern before anything runs. Installation can take up to 5 minutes for large packages; call PythonInfo " +
        "afterward to confirm what actually got installed. Use this before PythonRun needs a package that isn't " +
        "already present — check PythonInfo first if unsure what's installed.")]
    public string PythonInstallPackages(
        [Description("Space-separated pip requirement specifiers, e.g. 'requests' or 'numpy>=2 pandas==2.2.2'")] string packages)
    {
        var specs = (packages ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (specs.Length == 0) return "Error: specify at least one package.";
        // Requirement specifiers only — no pip options such as --index-url or -r.
        if (specs.FirstOrDefault(s => !RequirementSpec().IsMatch(s)) is { } invalid)
            return $"Error: '{invalid}' is not a valid package specifier.";

        if (EnsureVirtualEnvironment() is { } venvError) return venvError;

        var result = ProcessRunner.Run(VenvPython, ["-m", "pip", "install", "--disable-pip-version-check", .. specs],
            WorkingDirectory, InstallTimeout, environment: PythonEnvironment);
        if (result.TimedOut) return $"pip install timed out.\n{ProcessRunner.Tail(result.Output)}";

        // pip is chatty; the final lines say what was installed or why it failed.
        return result.ExitCode == 0
            ? ProcessRunner.Tail(result.Output, 1500)
            : $"pip install FAILED (exit code {result.ExitCode}).\n{ProcessRunner.Tail(result.Output)}";
    }

    [Description("Reports which Python interpreter PythonRun will use, its version, the virtual environment's " +
        "location, and the full list of packages currently installed in it (via pip list). If the virtual " +
        "environment hasn't been created yet, says so rather than listing packages. Use this to check whether a " +
        "package is already available before calling PythonInstallPackages, or to diagnose a PythonRun failure caused " +
        "by a missing interpreter or package. It makes no changes and runs only version/package-listing probes, not " +
        "arbitrary commands.")]
    public string PythonInfo()
    {
        if (ResolveInterpreter() is not { } python) return NotInstalledMessage;

        var version = ProcessRunner.Run(python.FileName, [.. python.PrefixArgs, "--version"], WorkingDirectory, ProbeTimeout);
        var sb = new StringBuilder();
        sb.AppendLine($"Interpreter: {python.FileName} {string.Join(' ', python.PrefixArgs)}".TrimEnd());
        sb.AppendLine($"Version: {version.Output.Trim()}");
        sb.AppendLine($"Virtual environment: {_venvDirectory} ({(File.Exists(VenvPython) ? "created" : "not created yet; PythonInstallPackages creates it")})");

        if (File.Exists(VenvPython))
        {
            var packages = ProcessRunner.Run(VenvPython, ["-m", "pip", "list", "--format=freeze", "--disable-pip-version-check"],
                WorkingDirectory, TimeSpan.FromMinutes(1), environment: PythonEnvironment);
            sb.AppendLine("Installed packages:");
            sb.Append(string.IsNullOrWhiteSpace(packages.Output) ? "(none)" : packages.Output.Trim());
        }

        return ProcessRunner.Truncate(sb.ToString().TrimEnd(), MaxOutputChars);
    }

    internal sealed record Interpreter(string FileName, string[] PrefixArgs);

    private const string NotInstalledMessage =
        "Python 3 is not installed or not on PATH. Install it (https://www.python.org/downloads/ or 'winget install Python.Python.3.12'), " +
        "or set PYTHON_PATH to the interpreter. On Windows, the 'python' Microsoft Store alias does not count as an install.";

    private string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

    private string VenvPython => OperatingSystem.IsWindows()
        ? Path.Combine(_venvDirectory, "Scripts", "python.exe")
        : Path.Combine(_venvDirectory, "bin", "python");

    /// <summary>The venv interpreter once it exists, otherwise the first working Python 3 on this machine.</summary>
    internal Interpreter? ResolveInterpreter() =>
        File.Exists(VenvPython) ? new Interpreter(VenvPython, []) : _baseInterpreter ??= FindBaseInterpreter();

    private Interpreter? FindBaseInterpreter()
    {
        var candidates = new List<Interpreter>();
        if (Environment.GetEnvironmentVariable("PYTHON_PATH") is { Length: > 0 } configured) candidates.Add(new(configured, []));
        if (OperatingSystem.IsWindows()) candidates.AddRange([new("py", ["-3"]), new("python", []), new("python3", [])]);
        else candidates.AddRange([new("python3", []), new("python", [])]);

        // Probing with --version also rejects the Windows Store stub, which prints an install hint and exits non-zero.
        return candidates.FirstOrDefault(c =>
        {
            var probe = ProcessRunner.Run(c.FileName, [.. c.PrefixArgs, "--version"], WorkingDirectory, ProbeTimeout);
            return probe.ExitCode == 0 && probe.Output.TrimStart().StartsWith("Python 3", StringComparison.Ordinal);
        });
    }

    private string? EnsureVirtualEnvironment()
    {
        if (File.Exists(VenvPython)) return null;
        if (ResolveInterpreter() is not { } python) return NotInstalledMessage;

        Directory.CreateDirectory(Path.GetDirectoryName(_venvDirectory)!);
        var result = ProcessRunner.Run(python.FileName, [.. python.PrefixArgs, "-m", "venv", _venvDirectory],
            WorkingDirectory, InstallTimeout, environment: PythonEnvironment);
        if (result.ExitCode != 0 || !File.Exists(VenvPython))
            return $"Error: could not create the virtual environment at {_venvDirectory} " +
                   $"(on Debian/Ubuntu install the 'python3-venv' package).\n{ProcessRunner.Tail(result.Output)}";

        _baseInterpreter = null;
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

    // name[extras] followed by optional version constraints, e.g. requests, uvicorn[standard], numpy>=2,<3
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*(\[[A-Za-z0-9._,-]+\])?([<>=!~]=?[A-Za-z0-9.*+!_-]+(,[<>=!~]=?[A-Za-z0-9.*+!_-]+)*)?$")]
    internal static partial Regex RequirementSpec();
}
