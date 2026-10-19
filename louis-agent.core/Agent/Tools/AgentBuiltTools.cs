namespace LouisAgent.Core.Agent.Tools;

using System.ComponentModel;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Scripts;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tools the agent builds for itself (<c>*.tool.md</c>): <see cref="CreateTool"/> makes one, active at once but only
/// pending; the user's <c>/approve</c> saves it for every session and <c>/reject</c> discards it; approved ones load at
/// start-up (<see cref="LoadApprovedTools"/>). Their scripts run with arguments as JSON on stdin, never in the code.
/// </summary>
/// <param name="registry">The engine's tools, which an agent-built tool joins and leaves.</param>
/// <param name="supportsTools">Whether the model takes tools at all; without them nothing is built or loaded.</param>
/// <param name="skillsDirectory">The skills folder, read when needed: hosts set it after the engine is built.</param>
/// <param name="reservedNames">The names no agent-built tool may take (CL15: the catalogue's tool names), read when needed, as the folder is.</param>
internal sealed class AgentBuiltTools(
    ToolRegistry registry, bool supportsTools, string workspaceRoot, PythonTools python, PowerShellTools powerShell,
    Func<string?> skillsDirectory, Func<IReadOnlySet<string>> reservedNames)
{
    private static readonly ILogger Log = AgentLogging.For<AgentBuiltTools>();

    private readonly Dictionary<string, ScriptTool> _scriptTools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised after agent-built tools are added or removed, so hosts that publish the tools can resync.</summary>
    public event Action? ToolsChanged;

    /// <summary>Approved agent-built tools live here (*.tool.md); new ones wait in its 'pending' subfolder.</summary>
    public string? ToolsDirectory => skillsDirectory() is { } skills ? Path.Combine(skills, "tools") : null;
    private string? PendingToolsDirectory => ToolsDirectory is null ? null : Path.Combine(ToolsDirectory, "pending");

    [Description("""
        Builds a new, typed tool from a markdown definition when no existing tool — built-in or already agent-built —
        can do what's needed and the capability is worth keeping, not for a single one-off action a skill or an
        inline script could handle just as well. The tool becomes callable immediately in this session; it is only
        saved for future sessions once the user explicitly approves it with '/approve <Name>' (reject with
        '/reject <Name>' to discard it). A name already used by a built-in tool, or an existing approved tool, is
        rejected. Definition format (markdown):
        # Tool: <PascalCaseName>
        - Description: <what it does and when to use it>
        - Timeout: <seconds, optional, default 60, max 600>
        - Parameters:
          - `argName` (string|integer|number|boolean, required|optional): <meaning>
        - Implementation:
        ```python
        import json, sys
        args = json.load(sys.stdin)   # validated arguments as a JSON object
        print(json.dumps({"result": ...}))
        ```
        Language: ```python, ```powershell (read input with [Console]::In.ReadToEnd() | ConvertFrom-Json) or ```bash.
        Arguments arrive as JSON on stdin (also as TOOL_ARG_<name> env vars) — never interpolate them into code or commands.
        Runs from the workspace root. Print the result (JSON preferred); exit non-zero with a message on failure.
        """)]
    public string CreateTool(
        [Description("The full tool definition markdown")] string definition,
        [Description("Set true to replace a pending tool you created earlier with the same name")] bool overwrite = false)
    {
        if (!supportsTools) return MessageText.Format(ToolMessage.ToolsNotSupported);
        if (PendingToolsDirectory is null) return MessageText.Format(ToolMessage.ToolBuildingUnavailable);

        var (parsed, errors) = ScriptToolDefinition.Parse(definition);
        if (parsed is null) return MessageText.Format(ToolMessage.ToolDefinitionInvalid, new { Errors = string.Join("\n- ", errors) });

        if (_scriptTools.TryGetValue(parsed.Name, out var existing))
        {
            if (existing.Approved) return MessageText.Format(ToolMessage.ApprovedToolExists, new { parsed.Name });
            if (!overwrite) return MessageText.Format(ToolMessage.PendingToolExists, new { parsed.Name });
        }
        else if (registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase) || reservedNames().Contains(parsed.Name))
        {
            return MessageText.Format(ToolMessage.BuiltInToolName, new { parsed.Name });
        }

        Directory.CreateDirectory(PendingToolsDirectory);
        File.WriteAllText(Path.Combine(PendingToolsDirectory, $"{parsed.Name}.tool.md"), definition);
        RegisterScriptTool(parsed, approved: false);

        string parameters = parsed.Parameters.Count == 0
            ? MessageText.Format(ToolMessage.NoParameters)
            : string.Join(", ", parsed.Parameters.Select(p => $"{p.Name}: {p.Type}{(p.Required ? "" : "?")}"));
        return MessageText.Format(ToolMessage.ToolCreated, new { parsed.Name, parsed.Language, Parameters = parameters });
    }

    [Description("Lists every agent-built tool known right now: approved ones that load in every session, ones " +
        "pending approval that are only active for this session, and — separately — pending tool files left on " +
        "disk from an earlier session that never got approved and aren't currently loaded. Each entry shows its " +
        "approval state and its own description. Use this before CreateTool to check whether a similar tool already " +
        "exists, or when the user asks what tools the agent has built for itself; it makes no changes.")]
    public string ListAgentTools()
    {
        var pendingOnDisk = PendingToolsDirectory is not null && Directory.Exists(PendingToolsDirectory)
            ? Directory.EnumerateFiles(PendingToolsDirectory, "*.tool.md").Select(f => Path.GetFileName(f)[..^".tool.md".Length]).ToList()
            : [];

        var lines = _scriptTools.Values.OrderBy(t => t.Name)
            .Select(t => MessageText.Format(t.Approved ? ToolMessage.AgentToolApproved : ToolMessage.AgentToolPending,
                new { t.Name, t.Definition.Description }))
            .Concat(pendingOnDisk.Where(n => !_scriptTools.ContainsKey(n)).Order()
                .Select(n => MessageText.Format(ToolMessage.AgentToolNotLoaded, new { Name = n })))
            .ToList();

        return lines.Count == 0 ? MessageText.Format(ToolMessage.NoAgentTools) : string.Join('\n', lines);
    }

    /// <summary>
    /// Loads approved tools from <see cref="ToolsDirectory"/>. Called once by the host after construction.
    /// Pending tools are deliberately not loaded: they only last for the session that created them.
    /// </summary>
    public int LoadApprovedTools()
    {
        if (!supportsTools || ToolsDirectory is null || !Directory.Exists(ToolsDirectory)) return 0;

        int loaded = 0;
        foreach (string file in Directory.EnumerateFiles(ToolsDirectory, "*.tool.md").Order())
        {
            var (parsed, errors) = ScriptToolDefinition.Parse(File.ReadAllText(file));
            if (parsed is null)
            {
                Log.Log(LogLevel.Warning, LogMessage.AgentToolInvalid, new { File = Path.GetFileName(file), Errors = string.Join("; ", errors) });
                continue;
            }

            if (registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase))
            {
                Log.Log(LogLevel.Warning, LogMessage.AgentToolNameInUse, new { File = Path.GetFileName(file), parsed.Name });
                continue;
            }

            RegisterScriptTool(parsed, approved: true);
            loaded++;
        }

        if (loaded > 0) Log.Log(LogLevel.Information, LogMessage.AgentToolsLoaded, new { Count = loaded, Directory = ToolsDirectory });
        return loaded;
    }


    /// <summary>Whether a tool of this name waits in pending/ (FX20: the engine checks before it routes /approve).</summary>
    internal bool HasPendingTool(string name) => PendingToolsDirectory is not null && File.Exists(Path.Combine(PendingToolsDirectory, $"{name}.tool.md"));

    internal string ApproveTool(string name)
    {
        if (PendingToolsDirectory is null || ToolsDirectory is null) return MessageText.Format(UserText.ToolBuildingNotAvailable);
        if (string.IsNullOrWhiteSpace(name)) return MessageText.Format(UserText.ApproveUsage);

        string pending = Path.Combine(PendingToolsDirectory, $"{name}.tool.md");
        if (!File.Exists(pending)) return MessageText.Format(UserText.NoPendingToolSeeTools, new { Name = name });

        var (parsed, errors) = ScriptToolDefinition.Parse(File.ReadAllText(pending));
        if (parsed is null) return MessageText.Format(UserText.CannotApprove, new { Name = name, Errors = string.Join("; ", errors) });

        string approvedPath = Path.Combine(ToolsDirectory, $"{parsed.Name}.tool.md");
        if (File.Exists(approvedPath)) return MessageText.Format(UserText.ApprovedToolAlreadyThere, new { parsed.Name, Path = approvedPath });

        File.Move(pending, approvedPath);
        if (_scriptTools.TryGetValue(parsed.Name, out var tool))
        {
            tool.Approved = true;
            ToolsChanged?.Invoke();
        }
        else if (supportsTools && !registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase))
            RegisterScriptTool(parsed, approved: true);

        return MessageText.Format(UserText.ToolApproved, new { parsed.Name, Path = approvedPath });
    }

    internal string RejectTool(string name)
    {
        if (PendingToolsDirectory is null) return MessageText.Format(UserText.ToolBuildingNotAvailable);
        if (string.IsNullOrWhiteSpace(name)) return MessageText.Format(UserText.RejectUsage);

        string pending = Path.Combine(PendingToolsDirectory, $"{name}.tool.md");
        bool hadFile = File.Exists(pending);
        if (hadFile) File.Delete(pending);

        bool wasActive = _scriptTools.TryGetValue(name, out var tool) && !tool.Approved;
        if (wasActive)
        {
            _scriptTools.Remove(name);
            registry.Remove(tool!);
            ToolsChanged?.Invoke();
        }

        return hadFile || wasActive
            ? MessageText.Format(UserText.ToolRejected, new { Name = name, Removed = (wasActive ? MessageText.Format(UserText.ToolRejectedFromSession) : "") })
            : MessageText.Format(UserText.NoPendingTool, new { Name = name });
    }

    private void RegisterScriptTool(ScriptToolDefinition definition, bool approved)
    {
        if (_scriptTools.Remove(definition.Name, out var previous)) registry.Remove(previous);

        var tool = new ScriptTool(definition, approved, RunScriptTool);
        _scriptTools[definition.Name] = tool;
        registry.Add(tool);
        ToolsChanged?.Invoke();
    }

    private string RunScriptTool(ScriptToolDefinition definition, string inputJson, IReadOnlyDictionary<string, string> env)
    {
        var timeout = TimeSpan.FromSeconds(definition.TimeoutSeconds);
        string? notInstalled = null;
        ProcessRunner.Result? result = definition.Language switch
        {
            "python" => python.RunScript(definition.Code, null, [], inputJson, timeout, out notInstalled, env),
            "powershell" => powerShell.RunScript(definition.Code, null, [], inputJson, timeout, out notInstalled, env),
            _ => ProcessRunner.Run(BashTools.ResolveShell(), ["-c", definition.Code], workspaceRoot, timeout, inputJson, env),
        };

        if (notInstalled is not null) return MessageText.Format(ToolMessage.ErrorWithReason, new { Reason = notInstalled });
        if (result!.TimedOut) return MessageText.Format(ToolMessage.AgentToolTimeout, new { definition.Name, Seconds = definition.TimeoutSeconds, Output = ProcessRunner.Tail(result.Output) });

        string output = ProcessRunner.Truncate(result.Output.Trim());
        if (result.ExitCode != 0) return MessageText.Format(ToolMessage.ExitCode, new { Code = result.ExitCode, Output = output });
        return string.IsNullOrWhiteSpace(output) ? MessageText.Format(ToolMessage.ToolRanNoOutput) : output;
    }

}
