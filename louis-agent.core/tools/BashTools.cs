namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Text;

/// <summary>
/// Bash toolset: run inline scripts or workspace .sh files with Git Bash on Windows or /bin/bash elsewhere.
/// Arguments become real positional parameters ($1, $2, ...) and scripts run in strict mode by default.
/// </summary>
public sealed class BashTools
{
    private const int MaxOutputChars = 12_000;
    private static readonly string[] CommonCommands = ["git", "curl", "jq", "grep", "find", "sed", "awk", "python3", "pwsh", "dotnet"];

    private readonly string _workspaceRoot;

    public BashTools(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    [Description("Run a bash script. Pass either an inline 'script' or a workspace-relative 'scriptPath' (.sh). Arguments are available as $1, $2, ... / \"$@\". Runs with 'set -euo pipefail' unless strict is false. Returns exit code and output.")]
    public string BashRun(
        [Description("Inline bash script to run (leave empty when using scriptPath)")] string script = "",
        [Description("Workspace-relative path to a .sh file to run (leave empty when using script)")] string scriptPath = "",
        [Description("Arguments passed as $1, $2, ...; space separated, wrap an argument in double quotes to keep spaces")] string arguments = "",
        [Description("Text sent to standard input (stdin is closed afterwards)")] string standardInput = "",
        [Description("Maximum seconds to run before the process is killed (default 60, max 600)")] int timeoutSeconds = 60,
        [Description("Run with 'set -euo pipefail' so the first failing command stops the script (default true)")] bool strict = true)
    {
        bool hasScript = !string.IsNullOrWhiteSpace(script);
        bool hasPath = !string.IsNullOrWhiteSpace(scriptPath);
        if (hasScript == hasPath) return "Error: pass exactly one of 'script' or 'scriptPath'.";

        string? file = null;
        if (hasPath)
        {
            if (!TryResolveWorkspacePath(scriptPath, out file, out var error)) return error;
            if (!File.Exists(file)) return $"Error: script '{scriptPath}' not found.";
        }

        var result = RunScript(hasScript ? script : null, file, DotNetTools.SplitArguments(arguments), standardInput,
            TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), strict, out string? notInstalled);
        if (notInstalled is not null) return notInstalled;

        var sb = new StringBuilder();
        sb.AppendLine(result!.TimedOut
            ? $"Script was still running after {timeoutSeconds}s and was killed."
            : $"Exit code: {result.ExitCode}");
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        // Linux bash treats the \r of Windows line endings as part of each command ("$'\r': command not found",
        // "syntax error near unexpected token"); Git Bash tolerates it, so only hint when the script failed.
        if (file is not null && result.ExitCode != 0 && File.ReadAllText(file).Contains('\r'))
            sb.Append("\nHint: the script has Windows (CRLF) line endings; convert it to LF.");
        return ProcessRunner.Truncate(sb.ToString(), MaxOutputChars);
    }

    [Description("Show which bash is used (Git Bash on Windows, /bin/bash elsewhere), its version and which common command-line tools are available.")]
    public string BashInfo()
    {
        if (FindBash() is not { } bash) return NotInstalledMessage;

        string probe = $"echo \"Version: $BASH_VERSION\"; echo Commands:; for c in {string.Join(' ', CommonCommands)}; do " +
                       "if command -v \"$c\" >/dev/null 2>&1; then echo \"  $c: available\"; else echo \"  $c: missing\"; fi; done";
        var result = ProcessRunner.Run(bash, ["-c", probe], WorkingDirectory, TimeSpan.FromSeconds(20));
        return $"Executable: {bash}\n{result.Output.Trim()}";
    }

    /// <summary>
    /// Runs inline script text (written to a temp .sh with LF endings) or an existing script file.
    /// Returns null with <paramref name="notInstalled"/> set when no usable bash exists.
    /// </summary>
    internal ProcessRunner.Result? RunScript(
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
            File.WriteAllText(tempFile, inlineScript.Replace("\r\n", "\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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

            var result = ProcessRunner.Run(bash, [.. options, "-c", Launcher, target], WorkingDirectory, timeout, standardInput, env);
            return tempFile is null ? result : result with { Output = result.Output.Replace(target, "<script>") };
        }
        finally
        {
            if (tempFile is not null)
            {
                try { File.Delete(tempFile); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }

    // $0 is the script path. Sourcing keeps the script's own file name and line numbers in error messages.
    private const string Launcher =
        """__a=(); for ((__i = 0; __i < LOUIS_AGENT_ARGC; __i++)); do __v="LOUIS_AGENT_ARG_$__i"; __a+=("${!__v}"); done; """ +
        """unset __i __v; source "$0" "${__a[@]+"${__a[@]}"}" """;

    private const string NotInstalledMessage =
        "Bash is not available. On Windows install Git for Windows (https://git-scm.com, includes Git Bash) " +
        "or set BASH_PATH to bash.exe. WSL's bash.exe in System32 is deliberately not used.";

    private static string? _bash;
    private static bool _searched;

    /// <summary>
    /// Locates bash: BASH_PATH/SKILL_SHELL override, then /bin/bash, or Git Bash on Windows. Never the WSL stub in
    /// System32, which starts a separate Linux environment and drops the process environment.
    /// </summary>
    internal static string? FindBash()
    {
        if (_searched) return _bash;

        foreach (var variable in new[] { "BASH_PATH", "SKILL_SHELL" })
        {
            if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } configured && File.Exists(configured))
                return Remember(configured);
        }

        if (!OperatingSystem.IsWindows())
            return Remember(File.Exists("/bin/bash") ? "/bin/bash" : File.Exists("/usr/bin/bash") ? "/usr/bin/bash" : null);

        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        ];
        var gitBash = roots.Where(r => r.Length > 0).Select(r => Path.Combine(r, "Git", "bin", "bash.exe")).FirstOrDefault(File.Exists);
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

    private string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

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
}
