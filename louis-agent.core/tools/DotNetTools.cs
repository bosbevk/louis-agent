namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// .NET SDK toolset: build, test, run and inspect projects inside the workspace.
/// Output is condensed (diagnostics, failed tests, stack traces) so the model sees what matters, not MSBuild noise.
/// </summary>
public sealed class DotNetTools
{
    private const int MaxOutputChars = 12_000;
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(10);

    // e.g. C:\src\Foo.cs(12,5): error CS1002: ; expected [C:\src\Foo.csproj]
    private static readonly Regex DiagnosticLine = new(
        @"^(?<text>.*?:\s*(?<kind>error|warning)\s+(?<code>[A-Za-z]+\d+)\s*:.*?)(?:\s+\[[^\]]+\])?\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly string _workspaceRoot;

    public DotNetTools(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    [Description("List .sln/.slnx and .csproj/.fsproj files in the workspace, to pick a target for build/test/run.")]
    public string DotNetListProjects()
    {
        string root = WorkingDirectory;
        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".sln") || f.EndsWith(".slnx") || f.EndsWith(".csproj") || f.EndsWith(".fsproj"))
            .Where(f => !IsBuildOutput(f))
            .Select(f => Path.GetRelativePath(root, f))
            .OrderBy(f => f.EndsWith(".sln") || f.EndsWith(".slnx") ? 0 : 1)
            .ThenBy(f => f)
            .ToList();
        return files.Count == 0 ? "No .NET solutions or projects found." : string.Join('\n', files);
    }

    [Description("Show installed .NET SDKs and runtimes.")]
    public string DotNetSdkInfo()
    {
        var sdks = RunDotNet(["--list-sdks"], TimeSpan.FromSeconds(30));
        var runtimes = RunDotNet(["--list-runtimes"], TimeSpan.FromSeconds(30));
        return $"SDKs:\n{sdks.Output.Trim()}\n\nRuntimes:\n{runtimes.Output.Trim()}";
    }

    [Description("Build a .NET solution or project and return a summary of errors and warnings (file(line,col): code: message).")]
    public string DotNetBuild(
        [Description("Relative path to a .sln/.csproj or folder; empty = workspace root")] string project = "",
        [Description("Build configuration, e.g. Debug or Release")] string configuration = "Debug",
        [Description("If true, include warnings in the summary (errors are always included)")] bool includeWarnings = true)
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;

        var result = RunDotNet(["build", .. target, "-c", configuration, "-nologo", "-nodeReuse:false", "-v:q", "-clp:NoSummary"], BuildTimeout);
        return SummarizeBuild(result, includeWarnings);
    }

    [Description("Restore NuGet packages for a solution or project.")]
    public string DotNetRestore(
        [Description("Relative path to a .sln/.csproj or folder; empty = workspace root")] string project = "")
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;
        var result = RunDotNet(["restore", .. target, "-nologo", "-v:q"], BuildTimeout);
        return SummarizeBuild(result, includeWarnings: true);
    }

    [Description("Clean build outputs for a solution or project.")]
    public string DotNetClean(
        [Description("Relative path to a .sln/.csproj or folder; empty = workspace root")] string project = "",
        [Description("Build configuration, e.g. Debug or Release")] string configuration = "Debug")
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;
        var result = RunDotNet(["clean", .. target, "-c", configuration, "-nologo", "-v:q"], BuildTimeout);
        return SummarizeBuild(result, includeWarnings: false);
    }

    [Description("Run tests and return the pass/fail summary plus details (message and stack trace) of failed tests. Builds first unless noBuild is true.")]
    public string DotNetTest(
        [Description("Relative path to a .sln/test .csproj or folder; empty = workspace root")] string project = "",
        [Description("Optional test filter, e.g. 'FullyQualifiedName~ParserTests' or 'Name=MyTest'")] string filter = "",
        [Description("Build configuration, e.g. Debug or Release")] string configuration = "Debug",
        [Description("If true, skip the build step (use after a successful Build)")] bool noBuild = false)
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;

        List<string> args = ["test", .. target, "-c", configuration, "--nologo", "-nodeReuse:false"];
        if (!string.IsNullOrWhiteSpace(filter)) args.AddRange(["--filter", filter]);
        if (noBuild) args.Add("--no-build");

        var result = RunDotNet(args, TestTimeout);
        return SummarizeTests(result);
    }

    [Description("Run a .NET project (console app) with optional arguments and return its output, exit code and any unhandled exception stack trace. Use to reproduce and debug runtime errors.")]
    public string DotNetRun(
        [Description("Relative path to the .csproj or its folder")] string project,
        [Description("Arguments passed to the program, space separated; wrap an argument in double quotes to keep spaces")] string arguments = "",
        [Description("Text sent to the program's standard input (stdin is closed afterwards)")] string standardInput = "",
        [Description("Maximum seconds to let the program run before it is killed (default 60)")] int timeoutSeconds = 60,
        [Description("Build configuration, e.g. Debug or Release")] string configuration = "Debug")
    {
        if (string.IsNullOrWhiteSpace(project)) return "Error: specify the project to run (see DotNetListProjects).";
        if (!TryResolveTarget(project, out var target, out var error)) return error;

        // Build separately so compile errors come back as a clean diagnostic summary.
        var build = RunDotNet(["build", .. target, "-c", configuration, "-nologo", "-nodeReuse:false", "-v:q", "-clp:NoSummary"], BuildTimeout);
        if (build.ExitCode != 0) return SummarizeBuild(build, includeWarnings: false);

        var args = new List<string> { "run", "--project", target[0], "-c", configuration, "--no-build" };
        var programArgs = SplitArguments(arguments);
        if (programArgs.Count > 0) args.AddRange(["--", .. programArgs]);

        var result = RunDotNet(args, TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600)), standardInput);
        var sb = new StringBuilder();
        sb.AppendLine(result.TimedOut
            ? $"Program was still running after {timeoutSeconds}s and was killed."
            : $"Exit code: {result.ExitCode}");
        sb.AppendLine("--- output ---");
        sb.Append(result.Output.Trim());
        return Truncate(sb.ToString());
    }

    [Description("List NuGet packages referenced by a project, optionally only outdated or vulnerable ones.")]
    public string DotNetListPackages(
        [Description("Relative path to a .sln/.csproj or folder; empty = workspace root")] string project = "",
        [Description("Filter: '' for all, 'outdated' or 'vulnerable'")] string filter = "")
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;

        List<string> args = ["list", .. target, "package"];
        switch (filter.Trim().ToLowerInvariant())
        {
            case "": break;
            case "outdated": args.Add("--outdated"); break;
            case "vulnerable": args.AddRange(["--vulnerable", "--include-transitive"]); break;
            default: return "Error: filter must be '', 'outdated' or 'vulnerable'.";
        }

        var result = RunDotNet(args, TimeSpan.FromMinutes(2));
        return Truncate(FormatResult(result));
    }

    [Description("Add a NuGet package reference to a project.")]
    public string DotNetAddPackage(
        [Description("Relative path to the .csproj or its folder")] string project,
        [Description("Package id, e.g. Newtonsoft.Json")] string packageId,
        [Description("Optional version; empty = latest stable")] string version = "")
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;
        List<string> args = ["add", .. target, "package", packageId];
        if (!string.IsNullOrWhiteSpace(version)) args.AddRange(["--version", version]);
        return Truncate(FormatResult(RunDotNet(args, TimeSpan.FromMinutes(2))));
    }

    [Description("Remove a NuGet package reference from a project.")]
    public string DotNetRemovePackage(
        [Description("Relative path to the .csproj or its folder")] string project,
        [Description("Package id to remove")] string packageId)
    {
        if (!TryResolveTarget(project, out var target, out var error)) return error;
        return Truncate(FormatResult(RunDotNet(["remove", .. target, "package", packageId], TimeSpan.FromMinutes(1))));
    }

    private string WorkingDirectory =>
        Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") is { Length: > 0 } dir ? dir : _workspaceRoot;

    /// <summary>Resolves a project/solution/folder path, refusing anything outside the working directory.</summary>
    internal bool TryResolveTarget(string project, out string[] target, out string error)
    {
        string root = Path.GetFullPath(WorkingDirectory);
        string full = Path.GetFullPath(Path.Combine(root, project ?? ""));
        target = [];
        error = "";

        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!full.Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            error = $"Error: '{project}' is outside the workspace.";
            return false;
        }

        if (!File.Exists(full) && !Directory.Exists(full))
        {
            error = $"Error: '{project}' not found. Use DotNetListProjects to see available projects.";
            return false;
        }

        target = [full];
        return true;
    }

    internal static string SummarizeBuild(ProcessRunner.Result result, bool includeWarnings)
    {
        if (result.TimedOut) return $"Timed out.\n{Tail(result.Output)}";

        var diagnostics = DiagnosticLine.Matches(result.Output)
            .Select(m => (Kind: m.Groups["kind"].Value, Text: m.Groups["text"].Value.Trim()))
            .Distinct()
            .ToList();
        var errors = diagnostics.Where(d => d.Kind == "error").Select(d => d.Text).ToList();
        var warnings = diagnostics.Where(d => d.Kind == "warning").Select(d => d.Text).ToList();

        var sb = new StringBuilder();
        sb.AppendLine(result.ExitCode == 0 ? "Succeeded." : $"FAILED (exit code {result.ExitCode}).");
        sb.AppendLine($"{errors.Count} error(s), {warnings.Count} warning(s).");
        foreach (var e in errors.Take(50)) sb.AppendLine(e);
        if (includeWarnings)
        {
            foreach (var w in warnings.Take(30)) sb.AppendLine(w);
            if (warnings.Count > 30) sb.AppendLine($"... {warnings.Count - 30} more warning(s)");
        }

        // A failure with no parsed diagnostics (e.g. missing SDK, bad path): show the raw tail instead.
        if (result.ExitCode != 0 && errors.Count == 0) sb.AppendLine(Tail(result.Output));
        return Truncate(sb.ToString().TrimEnd());
    }

    internal static string SummarizeTests(ProcessRunner.Result result)
    {
        if (result.TimedOut) return $"Test run timed out.\n{Tail(result.Output)}";

        var lines = result.Output.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        // Compile errors stop the run before any tests execute.
        if (DiagnosticLine.Matches(result.Output).Any(m => m.Groups["kind"].Value == "error"))
            return "Build failed before tests ran.\n" + SummarizeBuild(result, includeWarnings: false);

        var sb = new StringBuilder();
        foreach (var summary in lines.Where(l => Regex.IsMatch(l, @"^\s*(Passed!|Failed!|Total tests:|\s*Passed:|\s*Failed:|\s*Skipped:)")))
            sb.AppendLine(summary.Trim());

        // Each failed test block starts with "  Failed <name>" and runs until the next test result line.
        bool inFailure = false;
        int failures = 0;
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^\s{2}Failed\s+\S"))
            {
                if (++failures > 20) { sb.AppendLine("... more failures omitted"); break; }
                inFailure = true;
                sb.AppendLine().AppendLine(line.Trim());
                continue;
            }

            if (inFailure && Regex.IsMatch(line, @"^\s{2}(Passed|Skipped)\s+\S|^(Passed!|Failed!)|^Results File"))
            {
                inFailure = false;
                continue;
            }

            if (inFailure && !string.IsNullOrWhiteSpace(line)) sb.AppendLine(line);
        }

        if (sb.Length == 0) sb.AppendLine(result.ExitCode == 0 ? "Tests passed." : Tail(result.Output));
        else if (result.ExitCode != 0 && failures == 0) sb.AppendLine(Tail(result.Output));
        return Truncate(sb.ToString().TrimEnd());
    }

    /// <summary>Splits a command-line string on spaces, honouring double quotes.</summary>
    internal static List<string> SplitArguments(string arguments)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(arguments)) return result;

        var current = new StringBuilder();
        bool inQuotes = false;
        foreach (char c in arguments)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    private static readonly Dictionary<string, string> DotNetEnvironment = new()
    {
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
        ["DOTNET_NOLOGO"] = "1",
        ["MSBUILDTERMINALLOGGER"] = "off",
    };

    private ProcessRunner.Result RunDotNet(IEnumerable<string> args, TimeSpan timeout, string? standardInput = null) =>
        ProcessRunner.Run("dotnet", args, WorkingDirectory, timeout, standardInput, DotNetEnvironment);

    private static string FormatResult(ProcessRunner.Result result) =>
        result.TimedOut ? $"Timed out.\n{Tail(result.Output)}"
        : result.ExitCode == 0 ? (string.IsNullOrWhiteSpace(result.Output) ? "Success (no output)." : result.Output.Trim())
        : $"[Exit Code {result.ExitCode}] {result.Output.Trim()}";

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(p => p is "bin" or "obj" or "node_modules" or ".git");

    private static string Tail(string text) => ProcessRunner.Tail(text);

    private static string Truncate(string text) => ProcessRunner.Truncate(text, MaxOutputChars);
}
