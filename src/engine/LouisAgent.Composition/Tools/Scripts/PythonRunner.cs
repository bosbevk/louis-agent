using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools.Scripts.Messages;
using System.Text;

namespace LouisAgent.Composition.Tools.Scripts;

/// <summary>
/// Runs Python for whoever needs it: the python tools, and the engine's own skills and agent-built tools. It keeps a
/// virtual environment of its own, so installs never touch the system Python or the repository.
/// </summary>
/// <remarks>
/// Shared here because the engine runs skills whether or not a session has the python toolset, and a pack can't be
/// referenced by the engine. What the model calls stays in the pack; only running a script is common.
/// </remarks>
public sealed class PythonRunner
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(15);

    /// <summary>What every Python this starts runs with.</summary>
    public static readonly IReadOnlyDictionary<string, string> PythonEnvironment =
        new Dictionary<string, string>
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
    public PythonRunner(string workspaceRoot, string? venvDirectory = null)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _venvDirectory = Path.GetFullPath(venvDirectory ?? DefaultVenvDirectory());
    }

    private static string DefaultVenvDirectory() =>
        Environment.GetEnvironmentVariable("PYTHON_VENV") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "louis-agent",
                "python-venv");

    /// <summary>
    /// Runs inline code (written to a temp .py) or an existing script file. Shared with ExecuteSkill and agent-built tools.
    /// Returns null with <paramref name="notInstalled"/> set when no Python 3 is available.
    /// </summary>
    public ProcessRunner.Result? RunScript(
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
            foreach (var (key, value) in environment)
                env[key] = value;

        try
        {
            var result = ProcessRunner.Run(python.FileName,
                [.. python.PrefixArgs, tempFile ?? scriptFile!, .. arguments],
                WorkingDirectory, timeout, standardInput, env);
            // Inline snippets run from a temp file; show a stable name in tracebacks instead.
            return tempFile is null ? result : result with { Output = result.Output.Replace(tempFile, "<snippet>") };
        }
        finally
        {
            if (tempFile is not null) TryDelete(tempFile);
        }
    }

    /// <summary>A Python to start, and what goes before the script.</summary>
    public sealed record Interpreter(string FileName, string[] PrefixArgs);

    /// <summary>What to tell the model when there's no Python.</summary>
    public static string NotInstalledMessage => MessageText.Format(RunnerToolMessage.PythonNotInstalled);

    /// <summary>Where scripts run.</summary>
    public string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

    /// <summary>The virtual environment's Python.</summary>
    public string VenvPython => OperatingSystem.IsWindows()
        ? Path.Combine(_venvDirectory, "Scripts", "python.exe")
        : Path.Combine(_venvDirectory, "bin", "python");

    /// <summary>The venv interpreter once it exists, otherwise the first working Python 3 on this machine.</summary>
    public Interpreter? ResolveInterpreter() =>
        File.Exists(VenvPython) ? new Interpreter(VenvPython, []) : _baseInterpreter ??= FindBaseInterpreter();

    private Interpreter? FindBaseInterpreter()
    {
        var candidates = new List<Interpreter>();
        if (Environment.GetEnvironmentVariable("PYTHON_PATH") is { Length: > 0 } configured)
            candidates.Add(new(configured, []));
        if (OperatingSystem.IsWindows())
            candidates.AddRange([new("py", ["-3"]), new("python", []), new("python3", [])]);
        else candidates.AddRange([new("python3", []), new("python", [])]);

        // Probing with --version also rejects the Windows Store stub, which prints an install hint and exits non-zero.
        return candidates.FirstOrDefault(c =>
        {
            var probe = ProcessRunner.Run(c.FileName, [.. c.PrefixArgs, "--version"], WorkingDirectory, ProbeTimeout);
            return probe.ExitCode == 0 && probe.Output.TrimStart().StartsWith("Python 3", StringComparison.Ordinal);
        });
    }

    /// <summary>The virtual environment's folder.</summary>
    public string VenvDirectory => _venvDirectory;

    /// <summary>Looks again next time, once a virtual environment exists.</summary>
    public void ForgetBaseInterpreter() => _baseInterpreter = null;

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
