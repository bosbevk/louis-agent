using LouisAgent.Core.Skills.Languages;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Agent.Messages;

namespace LouisAgent.Core.Agent.EngineTools;
/// <summary>
/// The skill tools (<see cref="ExecuteSkill"/>, <see cref="CreateSkillFile"/>, <see cref="ReloadSkills"/>) and the skill
/// set they work on: the loaded skills and the system prompt's skill documentation, which a reload replaces. The engine
/// sends <see cref="Documentation"/> as each conversation's system prompt.
/// </summary>
internal sealed partial class SkillTools
{
    private const int ShellTimeoutMs = 30_000;

    private readonly AgentOptions _options;
    private readonly string _workspaceRoot;
    private readonly PythonTools _python;
    private readonly PowerShellTools _powerShell;
    private Dictionary<string, MarkdownSkillLoader.SkillDef> _skills;

    // Files created this session, so they load even when discovery wouldn't pick them up: a pending file (FX20) lives
    // here until /approve moves it, and the entry follows the move.
    private readonly List<string> _createdSkillFiles = [];

    public SkillTools(ISkillProvider skillProvider, AgentOptions options, string workspaceRoot, PythonTools python,
        PowerShellTools powerShell)
    {
        _skills = ToSkillMap(skillProvider);
        Documentation = AgentEngine.Normalise(skillProvider.Documentation);
        _options = options;
        _workspaceRoot = workspaceRoot;
        _python = python;
        _powerShell = powerShell;
    }

    /// <summary>Combined default instructions and skill documentation used as the system prompt.</summary>
    public string Documentation { get; private set; }

    /// <summary>The currently loaded skills (refreshed by <see cref="ReloadSkills"/>).</summary>
    public IReadOnlyCollection<MarkdownSkillLoader.SkillDef> Skills => _skills.Values;

    /// <summary>The skills folder; <see cref="CreateSkillFile"/> writes into its agent/functions/pending/; null disables skill authoring.</summary>
    public string? SkillsDirectory { get; set; }

    /// <summary>Rebuilds the skill set from disk for <see cref="ReloadSkills"/>; null disables reloading.</summary>
    public Func<ISkillProvider>? SkillReloader { get; set; }

    [Description("Runs a named markdown skill — a reusable bash/PowerShell/Python procedure loaded from the Skills " +
                 "folder — passing jsonArgs as key-value pairs that the skill receives as SKILL_ARG_* environment variables, " +
                 "never spliced into the script text, so model-supplied values can't be interpreted as shell syntax. Skills " +
                 "are for repeatable, pre-authored procedures (e.g. Paymo lookups, code-review checklists); if a dedicated " +
                 "tool already exists for what you're trying to do, call that tool directly instead of going through a skill " +
                 "with the same effect. If the named skill isn't found, the error lists every skill name currently loaded so " +
                 "you can pick a valid one.")]
    public string ExecuteSkill(
        [Description("The exact name of the skill to execute (e.g., 'ListParentTasks').")]
        string skillName,
        [Description("JSON object of arguments for the skill (e.g., {\"project_id\":\"123\"}).")]
        string jsonArgs = "{}")
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return MessageText.Format(AgentToolMessage.SkillNameEmpty);
        }

        skillName = skillName.Trim();

        if (!_skills.TryGetValue(skillName, out var skill))
        {
            return MessageText.Format(AgentToolMessage.SkillNotFound,
                new { Name = skillName, Available = string.Join(", ", _skills.Keys) });
        }

        try
        {
            // Values are handed to the shell as environment variables and referenced as "${SKILL_ARG_x}",
            // so model-supplied text is never parsed as shell syntax.
            var env = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_options.PaymoApiKey))
            {
                env["PAYMO_API_KEY"] = _options.PaymoApiKey;
                env["PAYMO_API_KEY_BASE64"] =
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.PaymoApiKey}:X"));
            }

            if (!string.IsNullOrWhiteSpace(_options.DevopsApiKey))
            {
                env["DEVOPS_API_KEY"] = _options.DevopsApiKey;
            }

            if (!string.IsNullOrWhiteSpace(jsonArgs))
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonArgs);
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return MessageText.Format(AgentToolMessage.SkillArgumentsNotObject);
                    }

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        env[$"SKILL_ARG_{EnvNameSanitizer().Replace(prop.Name, "_")}"] =
                            prop.Value.ValueKind == JsonValueKind.String
                                ? prop.Value.GetString() ?? string.Empty
                                : prop.Value.GetRawText();
                    }
                }
                catch (JsonException jsonEx)
                {
                    return MessageText.Format(AgentToolMessage.SkillArgumentsInvalid, new { Reason = jsonEx.Message });
                }
            }

            // {{name}} placeholders become environment-variable reads in the skill's own language.
            string resolvedCommand = PlaceholderPattern().Replace(skill.Command, m =>
            {
                string name = m.Groups[1].Value;
                string variable = name is "PAYMO_API_KEY" or "PAYMO_API_KEY_BASE64" ? name : $"SKILL_ARG_{name}";
                return skill.Language switch
                {
                    ScriptLanguage.PowerShell => $"$env:{variable}",
                    ScriptLanguage.Python => $"__import__('os').environ.get('{variable}', '')",
                    _ => $"${{{variable}}}",
                };
            }).Trim();

            if (string.IsNullOrWhiteSpace(resolvedCommand))
            {
                return MessageText.Format(AgentToolMessage.SkillTemplateEmpty, new { Name = skillName });
            }

            return skill.Language switch
            {
                ScriptLanguage.PowerShell => FormatSkillResult(_powerShell.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPowerShell, env), noPowerShell),
                ScriptLanguage.Python => FormatSkillResult(_python.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPython, env), noPython),
                _ => RunShellCommand(resolvedCommand, env, _workspaceRoot),
            };
        }
        catch (Exception ex)
        {
            return MessageText.Format(AgentToolMessage.SkillFailed, new { Name = skillName, Reason = ex.Message });
        }
    }

    [Description("""
                 Creates (or updates, with overwrite=true) a reusable skill file, saved as agent/functions/pending/<name>.md in the Skills
                 folder and loaded at once, so its skills are callable via ExecuteSkill in this same session. It is only kept for later
                 sessions once the user approves it: ask them to type '/approve <name>' (or '/reject <name>' to discard it). With
                 overwrite, the new version waits for approval while the approved one stays for other sessions. Use this when the user
                 asks to save a repeatable procedure as a skill, not for a one-off action that only needs to happen once. A built-in
                 prompt's name (a toolset's guide) can't be taken with this tool. Markdown format, one section per skill:
                 # <Title>
                 <guidance for when/how to use these skills>
                 ## Skill: <PascalCaseName>
                 - Description: <one line>
                 - Parameters:
                   - `argName` (required|optional): <meaning>
                 - Execution:
                 ```bash
                 python3 script.py "${SKILL_ARG_argName}"
                 ```
                 The code fence language picks the interpreter: ```bash (default), ```powershell or ```python. Code runs from the
                 workspace root (30s limit). Read arguments only from environment variables, never by pasting values into code:
                 bash "${SKILL_ARG_name}" (quoted), PowerShell $env:SKILL_ARG_name, Python os.environ['SKILL_ARG_name'].
                 Guidance-only files (no Skill sections) are allowed.
                 """)]
    public string CreateSkillFile(
        [Description(
            "Short kebab-case name; the file is saved as agent/functions/pending/<name>.md until approved (e.g. 'csv-reports')")]
        string name,
        [Description("Full markdown content of the skills file")]
        string markdown,
        [Description(
            "Set true to replace an existing file (approved or pending) with this name; the new version waits for approval")]
        bool overwrite = false)
    {
        if (SkillsDirectory is null) return MessageText.Format(AgentToolMessage.SkillAuthoringUnavailable);
        name = (name ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith("-skills")) name = name[..^"-skills".Length];
        if (!SkillFileName().IsMatch(name)) return MessageText.Format(AgentToolMessage.SkillFileNameInvalid);
        if (string.IsNullOrWhiteSpace(markdown)) return MessageText.Format(AgentToolMessage.SkillMarkdownEmpty);

        string fileName = SkillFiles.Function(name);
        // A toolset's guide (toolsets/{name}.md, CL8) keeps its name too, so a skill file never passes for one.
        if (SkillFiles.IsBuiltIn(fileName) || SkillFiles.IsBuiltIn(SkillFiles.Guide(name)))
            return MessageText.Format(AgentToolMessage.SkillFileBuiltIn, new { File = fileName });

        // FX20: the file waits in pending/ for the user's /approve; an approved or pending one of the name needs overwrite.
        string path = PendingPath(name);
        if ((File.Exists(path) || File.Exists(ApprovedPath(name))) && !overwrite)
            return MessageText.Format(AgentToolMessage.SkillFileExists, new { File = fileName });

        // Validate before saving so a malformed section is reported instead of silently skipped.
        string tempPath = Path.Combine(Path.GetTempPath(), $"skill-check-{Guid.NewGuid():N}.md");
        File.WriteAllText(tempPath, markdown);
        List<MarkdownSkillLoader.SkillDef> parsed;
        try
        {
            parsed = MarkdownSkillLoader.Load(tempPath);
        }
        finally
        {
            File.Delete(tempPath);
        }

        int declared = SkillHeader().Matches(markdown).Count;
        if (parsed.Count < declared)
            return MessageText.Format(AgentToolMessage.SkillSectionsInvalid,
                new { Declared = declared, Valid = parsed.Count });

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, markdown);
        if (!_createdSkillFiles.Contains(path, StringComparer.OrdinalIgnoreCase)) _createdSkillFiles.Add(path);

        string reload = ReloadSkills();
        string skills = parsed.Count == 0
            ? MessageText.Format(AgentToolMessage.SkillGuidanceOnly)
            : string.Join(", ", parsed.Select(s => s.Name));
        return MessageText.Format(AgentToolMessage.SkillFileSaved,
            new { Path = path, Skills = skills, Name = name, Reload = reload });
    }

    // ---- FX20: the user's side of a pending skill file, reached through AgentEngine.HandleUserCommand, never the model ----

    // The name as CreateSkillFile saved it, lowercase (review of PR #68): on Linux "/approve Greeting" must still find greeting.md.
    private string PendingPath(string name) => Path.Combine(SkillsDirectory!,
        SkillFiles.PendingFunction(name.Trim().ToLowerInvariant()).Replace('/', Path.DirectorySeparatorChar));

    private string ApprovedPath(string name) => Path.Combine(SkillsDirectory!,
        SkillFiles.Function(name.Trim().ToLowerInvariant()).Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Whether a skill file of this name waits in pending/.</summary>
    internal bool HasPendingSkillFile(string name) => SkillsDirectory is not null && File.Exists(PendingPath(name));

    /// <summary>
    /// <c>/approve {name}</c>: the pending file moves over the approved one, so every later session loads it. No reload:
    /// this engine already runs it, and the entry in its own list follows the move so a later reload finds it once.
    /// </summary>
    internal string ApproveSkillFile(string name)
    {
        if (SkillsDirectory is null) return MessageText.Format(AgentToolMessage.SkillAuthoringUnavailable);
        string pending = PendingPath(name);
        if (!File.Exists(pending)) return MessageText.Format(UserText.NoPendingToolSeeTools, new { Name = name });

        string approved = ApprovedPath(name);
        File.Move(pending, approved, overwrite: true);
        int index = _createdSkillFiles.FindIndex(f => string.Equals(f, pending, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) _createdSkillFiles[index] = approved;
        return MessageText.Format(UserText.SkillFileApproved, new { Name = name, Path = approved });
    }

    /// <summary><c>/reject {name}</c>: the pending file is deleted, and this engine unloads it if it had it.</summary>
    internal string RejectSkillFile(string name)
    {
        if (SkillsDirectory is null) return MessageText.Format(AgentToolMessage.SkillAuthoringUnavailable);
        string pending = PendingPath(name);
        if (!File.Exists(pending)) return MessageText.Format(UserText.NoPendingTool, new { Name = name });

        File.Delete(pending);
        bool wasActive =
            _createdSkillFiles.RemoveAll(f => string.Equals(f, pending, StringComparison.OrdinalIgnoreCase)) > 0;
        if (wasActive) ReloadSkills();
        return MessageText.Format(UserText.SkillFileRejected, new
        {
            Name = name,
            Path = pending,
            Removed = wasActive ? MessageText.Format(UserText.SkillFileRejectedFromSession) : "",
        });
    }

    /// <summary>For <c>/tools</c>: one line per skill file in pending/, saying whether this engine runs it; empty when none.</summary>
    internal string ListPendingSkillFiles()
    {
        if (SkillsDirectory is null) return "";
        string folder = Path.Combine(SkillsDirectory,
            SkillFiles.PendingFunctionsFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(folder)) return "";
        var lines = Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.OrdinalIgnoreCase).Select(file =>
            MessageText.Format(
                _createdSkillFiles.Contains(file, StringComparer.OrdinalIgnoreCase)
                    ? UserText.PendingSkillFileActive
                    : UserText.PendingSkillFileNotLoaded,
                new { Name = Path.GetFileNameWithoutExtension(file) }));
        return string.Join('\n', lines);
    }

    [Description("Re-reads every skill file from disk — including any created earlier this session with " +
                 "CreateSkillFile — rebuilding the full skill set and the system prompt's skill documentation from scratch, " +
                 "then returns the names of every skill now available. Use this after a skills file has been edited outside " +
                 "the agent (e.g. by the user, or by another tool) so the change takes effect without restarting the " +
                 "session; CreateSkillFile already reloads automatically, so there's no need to call this right after it. " +
                 "Returns an error if the host wasn't configured with a skill reloader.")]
    public string ReloadSkills()
    {
        if (SkillReloader is null) return MessageText.Format(AgentToolMessage.SkillReloadUnavailable);

        var provider = new CompositeSkillProvider(SkillReloader());
        string loadedDocs = provider.Documentation;
        foreach (string file in _createdSkillFiles.Where(File.Exists))
        {
            // Skip files the reloader already picked up (e.g. via skills-folder discovery).
            if (!loadedDocs.Contains(File.ReadAllText(file).Trim(), StringComparison.Ordinal))
                provider.AddProvider(new MarkdownSkillProvider(file));
        }

        _skills = ToSkillMap(provider);
        Documentation = AgentEngine.Normalise(provider.Documentation);
        return MessageText.Format(AgentToolMessage.SkillsReloaded,
            new { Skills = _skills.Count == 0 ? "(none)" : string.Join(", ", _skills.Keys.Order()) });
    }

    private static Dictionary<string, MarkdownSkillLoader.SkillDef> ToSkillMap(ISkillProvider provider) =>
        provider.Skills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);


    /// <summary>Same result shape as <see cref="RunShellCommand"/> so skills read alike whatever their language.</summary>
    private static string FormatSkillResult(ProcessRunner.Result? result, string? notInstalled)
    {
        if (notInstalled is not null)
            return MessageText.Format(ToolMessage.ErrorWithReason, new { Reason = notInstalled });
        if (result!.TimedOut)
            return MessageText.Format(AgentToolMessage.CommandTimeout, new { Milliseconds = ShellTimeoutMs });

        string output = result.Output.Trim();
        if (result.ExitCode != 0)
            return MessageText.Format(AgentToolMessage.ExitCodeError, new { Code = result.ExitCode, Output = output });
        return string.IsNullOrWhiteSpace(output) ? MessageText.Format(AgentToolMessage.CommandRanNoOutput) : output;
    }


    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SkillFileName();

    [GeneratedRegex(@"^\s*##\s*Skill:", RegexOptions.Multiline)]
    private static partial Regex SkillHeader();


    private static string RunShellCommand(string command, Dictionary<string, string> env, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = BashTools.ResolveShell(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);
        foreach (var (key, value) in env) startInfo.Environment[key] = value;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read asynchronously so a chatty command cannot fill the pipe and deadlock WaitForExit.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ShellTimeoutMs))
        {
            process.Kill(entireProcessTree: true);
            return MessageText.Format(AgentToolMessage.CommandTimeout, new { Milliseconds = ShellTimeoutMs });
        }

        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            return MessageText.Format(AgentToolMessage.ExitCodeError,
                new { Code = process.ExitCode, Output = error.Trim() });
        }

        return string.IsNullOrWhiteSpace(output)
            ? MessageText.Format(AgentToolMessage.CommandRanNoOutput)
            : output.Trim();
    }


    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex EnvNameSanitizer();
}
