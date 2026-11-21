using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools.Scripts.Messages;
using System.Text;

namespace LouisAgent.Composition.Tools.Scripts;

/// <summary>
/// Runs bash for whoever needs it: the bash tools, and the engine's own skills and agent-built tools. Git Bash on
/// Windows, /bin/bash elsewhere, and never the WSL stub.
/// </summary>
/// <remarks>
/// Shared here because the engine runs skills whether or not a session has the toolset, and a pack can't be referenced
/// by the engine. What the model calls stays in the pack; only running a script is common.
/// </remarks>
public sealed class BashRunner
{
    private readonly string _workspaceRoot;

    /// <summary>A runner over this workspace.</summary>
    public BashRunner(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    /// <summary>
    /// Runs inline script text (written to a temp .sh with LF endings) or an existing script file.
    /// Returns null with <paramref name="notInstalled"/> set when no usable bash exists.
    /// </summary>
    public ProcessRunner.Result? RunScript(
        string? inlineScript,
        string? scriptFile,
        IReadOnlyList<string> arguments,
        string? standardInput,
        TimeSpan timeout,
        bool strict,
        out string? notInstalled,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        notInstalled = null;
        if (FindBash() is not { } bash)
        {
            notInstalled = NotInstalledMessage;
            return null;
        }

        string? tempFile = null;
        if (inlineScript is not null)
        {
            string tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "louis-agent-bash")).FullName;
            tempFile = Path.Combine(tempDir, $"script_{Guid.NewGuid():N}.sh");
            File.WriteAllText(tempFile, inlineScript.Replace("\r\n", "\n"),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        try
        {
            // Strict mode via options rather than a prepended line, so error line numbers match the script.
            string[] options = strict ? ["-o", "errexit", "-o", "nounset", "-o", "pipefail"] : [];
            // Forward slashes: Git Bash accepts C:/... paths reliably.
            string target = (tempFile ?? scriptFile!).Replace('\\', '/');

            // Script arguments travel in environment variables and the launcher rebuilds "$@" from them: Git Bash
            // re-parses its Windows command line (quotes, globs), so argv would not arrive verbatim.
            var env = new Dictionary<string, string>(environment ?? new Dictionary<string, string>())
            {
                ["LOUIS_AGENT_ARGC"] = arguments.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            for (int i = 0; i < arguments.Count; i++) env[$"LOUIS_AGENT_ARG_{i}"] = arguments[i];

            var result = ProcessRunner.Run(bash, [.. options, "-c", Launcher, target], WorkingDirectory, timeout,
                standardInput, env);
            return tempFile is null ? result : result with { Output = result.Output.Replace(target, "<script>") };
        }
        finally
        {
            if (tempFile is not null)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    // $0 is the script path. Sourcing keeps the script's own file name and line numbers in error messages.
    private const string Launcher =
        """__a=(); for ((__i = 0; __i < LOUIS_AGENT_ARGC; __i++)); do __v="LOUIS_AGENT_ARG_$__i"; __a+=("${!__v}"); done; """ +
        """unset __i __v; source "$0" "${__a[@]+"${__a[@]}"}" """;

    /// <summary>What to tell the model when there's no bash.</summary>
    public static string NotInstalledMessage => MessageText.Format(RunnerToolMessage.BashNotInstalled);

    private static string? _bash;
    private static bool _searched;

    /// <summary>
    /// The bash to start: <see cref="FindBash"/>, else the name the OS would find. Skills and agent-built tools use it,
    /// so they run the same (Git) bash as BashRun, never the WSL stub.
    /// </summary>
    public static string ResolveShell() => FindBash() ?? (OperatingSystem.IsWindows() ? "bash" : "/bin/bash");

    /// <summary>
    /// Locates bash: BASH_PATH/SKILL_SHELL override, then /bin/bash, or Git Bash on Windows. Never the WSL stub in
    /// System32, which starts a separate Linux environment and drops the process environment.
    /// </summary>
    public static string? FindBash()
    {
        if (_searched) return _bash;

        foreach (var variable in new[] { "BASH_PATH", "SKILL_SHELL" })
        {
            if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } configured && File.Exists(configured))
                return Remember(configured);
        }

        if (!OperatingSystem.IsWindows())
            return Remember(File.Exists("/bin/bash") ? "/bin/bash" :
                File.Exists("/usr/bin/bash") ? "/usr/bin/bash" : null);

        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        ];
        var gitBash = roots.Where(r => r.Length > 0).Select(r => Path.Combine(r, "Git", "bin", "bash.exe"))
            .FirstOrDefault(File.Exists);
        return Remember(gitBash ?? FindGitBashNextToGitOnPath());

        static string? Remember(string? path)
        {
            _bash = path;
            _searched = true;
            return path;
        }
    }

    // git.exe lives in <Git>\cmd or <Git>\bin; bash.exe is in <Git>\bin.
    private static string? FindGitBashNextToGitOnPath() =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Where(dir => File.Exists(Path.Combine(dir, "git.exe")))
        .Select(dir => Path.GetFullPath(Path.Combine(dir, "..", "bin", "bash.exe")))
        .FirstOrDefault(File.Exists);

    /// <summary>Where scripts run.</summary>
    public string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;
}
