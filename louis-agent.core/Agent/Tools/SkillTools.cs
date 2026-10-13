namespace LouisAgent.Core.Agent.Tools;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Messages;

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

    // Files created this session, so they load even when the agent function's skill set wouldn't pick them up.
    private readonly List<string> _createdSkillFiles = [];

    public SkillTools(ISkillProvider skillProvider, AgentOptions options, string workspaceRoot, PythonTools python, PowerShellTools powerShell)
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

    /// <summary>Folder where <see cref="CreateSkillFile"/> writes new *-skills.md files; null disables skill authoring.</summary>
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
        [Description("The exact name of the skill to execute (e.g., 'ListParentTasks').")] string skillName,
        [Description("JSON object of arguments for the skill (e.g., {\"project_id\":\"123\"}).")] string jsonArgs = "{}")
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return MessageText.Format(ToolMessage.SkillNameEmpty);
        }

        skillName = skillName.Trim();

        if (!_skills.TryGetValue(skillName, out var skill))
        {
            return MessageText.Format(ToolMessage.SkillNotFound, new { Name = skillName, Available = string.Join(", ", _skills.Keys) });
        }

        try
        {
            // Values are handed to the shell as environment variables and referenced as "${SKILL_ARG_x}",
            // so model-supplied text is never parsed as shell syntax.
            var env = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_options.PaymoApiKey))
            {
                env["PAYMO_API_KEY"] = _options.PaymoApiKey;
                env["PAYMO_API_KEY_BASE64"] = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.PaymoApiKey}:X"));
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
                        return MessageText.Format(ToolMessage.SkillArgumentsNotObject);
                    }

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        env[$"SKILL_ARG_{EnvNameSanitizer().Replace(prop.Name, "_")}"] =
                            prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.GetRawText();
                    }
                }
                catch (JsonException jsonEx)
                {
                    return MessageText.Format(ToolMessage.SkillArgumentsInvalid, new { Reason = jsonEx.Message });
                }
            }

            // {{name}} placeholders become environment-variable reads in the skill's own language.
            string resolvedCommand = PlaceholderPattern().Replace(skill.Command, m =>
            {
                string name = m.Groups[1].Value;
                string variable = name is "PAYMO_API_KEY" or "PAYMO_API_KEY_BASE64" ? name : $"SKILL_ARG_{name}";
                return skill.Language switch
                {
                    "powershell" => $"$env:{variable}",
                    "python" => $"__import__('os').environ.get('{variable}', '')",
                    _ => $"${{{variable}}}",
                };
            }).Trim();

            if (string.IsNullOrWhiteSpace(resolvedCommand))
            {
                return MessageText.Format(ToolMessage.SkillTemplateEmpty, new { Name = skillName });
            }

            return skill.Language switch
            {
                "powershell" => FormatSkillResult(_powerShell.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPowerShell, env), noPowerShell),
                "python" => FormatSkillResult(_python.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPython, env), noPython),
                _ => RunShellCommand(resolvedCommand, env, _workspaceRoot),
            };
        }
        catch (Exception ex)
        {
            return MessageText.Format(ToolMessage.SkillFailed, new { Name = skillName, Reason = ex.Message });
        }
    }

    [Description("""
        Creates (or updates, with overwrite=true) a reusable *-skills.md file under the Skills folder and loads it
        immediately so its skills become callable via ExecuteSkill in this same session — the file is saved to disk
        right away, but only becomes part of every future session's skill set if the agent's AGENT_FUNCTION setting
        loads it (e.g. 'louis', which loads every skill file). Use this when the user asks to save a repeatable
        procedure as a skill, not for a one-off action that only needs to happen once. A built-in skills file name
        (including 'default') cannot be overwritten with this tool. Markdown format, one section per skill:
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
        [Description("Short kebab-case name; the file is saved as <name>-skills.md (e.g. 'csv-reports')")] string name,
        [Description("Full markdown content of the skills file")] string markdown,
        [Description("Set true to replace an existing file previously created with this name")] bool overwrite = false)
    {
        if (SkillsDirectory is null) return MessageText.Format(ToolMessage.SkillAuthoringUnavailable);
        name = (name ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith("-skills")) name = name[..^"-skills".Length];
        if (!SkillFileName().IsMatch(name)) return MessageText.Format(ToolMessage.SkillFileNameInvalid);
        if (string.IsNullOrWhiteSpace(markdown)) return MessageText.Format(ToolMessage.SkillMarkdownEmpty);

        string fileName = $"{name}-skills.md";
        if (name == "default" || SkillFiles.BuiltIn.Contains(fileName))
            return MessageText.Format(ToolMessage.SkillFileBuiltIn, new { File = fileName });

        string path = Path.Combine(SkillsDirectory, fileName);
        if (File.Exists(path) && !overwrite)
            return MessageText.Format(ToolMessage.SkillFileExists, new { File = fileName });

        // Validate before saving so a malformed section is reported instead of silently skipped.
        string tempPath = Path.Combine(Path.GetTempPath(), $"skill-check-{Guid.NewGuid():N}.md");
        File.WriteAllText(tempPath, markdown);
        List<MarkdownSkillLoader.SkillDef> parsed;
        try { parsed = MarkdownSkillLoader.Load(tempPath); }
        finally { File.Delete(tempPath); }

        int declared = SkillHeader().Matches(markdown).Count;
        if (parsed.Count < declared)
            return MessageText.Format(ToolMessage.SkillSectionsInvalid, new { Declared = declared, Valid = parsed.Count });

        File.WriteAllText(path, markdown);
        if (!_createdSkillFiles.Contains(path, StringComparer.OrdinalIgnoreCase)) _createdSkillFiles.Add(path);

        string reload = ReloadSkills();
        string skills = parsed.Count == 0 ? MessageText.Format(ToolMessage.SkillGuidanceOnly) : string.Join(", ", parsed.Select(s => s.Name));
        return MessageText.Format(ToolMessage.SkillFileSaved, new { Path = path, Skills = skills, Reload = reload });
    }

    [Description("Re-reads every skill file from disk — including any created earlier this session with " +
        "CreateSkillFile — rebuilding the full skill set and the system prompt's skill documentation from scratch, " +
        "then returns the names of every skill now available. Use this after a skills file has been edited outside " +
        "the agent (e.g. by the user, or by another tool) so the change takes effect without restarting the " +
        "session; CreateSkillFile already reloads automatically, so there's no need to call this right after it. " +
        "Returns an error if the host wasn't configured with a skill reloader.")]
    public string ReloadSkills()
    {
        if (SkillReloader is null) return MessageText.Format(ToolMessage.SkillReloadUnavailable);

        var provider = new CompositeSkillProvider(SkillReloader());
        string loadedDocs = provider.Documentation;
        foreach (string file in _createdSkillFiles.Where(File.Exists))
        {
            // Skip files the reloader already picked up (e.g. via skills-folder discovery).
            if (!loadedDocs.Contains(File.ReadAllText(file).Trim(), StringComparison.Ordinal))
                provider.AddProvider(new MarkdownSkillProvider(file, includeDefaultInstructions: false));
        }

        _skills = ToSkillMap(provider);
        Documentation = AgentEngine.Normalise(provider.Documentation);
        return MessageText.Format(ToolMessage.SkillsReloaded, new { Skills = _skills.Count == 0 ? "(none)" : string.Join(", ", _skills.Keys.Order()) });
    }

    private static Dictionary<string, MarkdownSkillLoader.SkillDef> ToSkillMap(ISkillProvider provider) =>
        provider.Skills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);


    /// <summary>Same result shape as <see cref="RunShellCommand"/> so skills read alike whatever their language.</summary>
    private static string FormatSkillResult(ProcessRunner.Result? result, string? notInstalled)
    {
        if (notInstalled is not null) return MessageText.Format(ToolMessage.ErrorWithReason, new { Reason = notInstalled });
        if (result!.TimedOut) return MessageText.Format(ToolMessage.CommandTimeout, new { Milliseconds = ShellTimeoutMs });

        string output = result.Output.Trim();
        if (result.ExitCode != 0) return MessageText.Format(ToolMessage.ExitCodeError, new { Code = result.ExitCode, Output = output });
        return string.IsNullOrWhiteSpace(output) ? MessageText.Format(ToolMessage.CommandRanNoOutput) : output;
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
            return MessageText.Format(ToolMessage.CommandTimeout, new { Milliseconds = ShellTimeoutMs });
        }

        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            return MessageText.Format(ToolMessage.ExitCodeError, new { Code = process.ExitCode, Output = error.Trim() });
        }

        return string.IsNullOrWhiteSpace(output) ? MessageText.Format(ToolMessage.CommandRanNoOutput) : output.Trim();
    }


    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex EnvNameSanitizer();
}
