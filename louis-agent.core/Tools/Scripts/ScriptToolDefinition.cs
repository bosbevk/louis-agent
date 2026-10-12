namespace LouisAgent.Core.Tools.Scripts;

using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Messages;

/// <summary>
/// An agent-built tool defined in a *.tool.md file: name, description, typed parameters and a script implementation
/// (python, powershell or bash) that receives its arguments as a JSON object on stdin.
/// </summary>
public sealed partial record ScriptToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<ScriptToolParameter> Parameters,
    string Language,
    string Code,
    int TimeoutSeconds)
{
    public const int DefaultTimeoutSeconds = 60;
    private static readonly string[] SupportedTypes = ["string", "integer", "number", "boolean"];

    /// <summary>Parses a tool definition, returning every problem found so the author can fix them in one go.</summary>
    public static (ScriptToolDefinition? Definition, List<string> Errors) Parse(string markdown)
    {
        var errors = new List<string>();
        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');

        string? name = null, description = null, language = null, code = null;
        int timeout = DefaultTimeoutSeconds;
        var parameters = new List<ScriptToolParameter>();
        bool inParameters = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd();
            string trimmed = line.Trim();

            if (ToolHeader().Match(trimmed) is { Success: true } header)
            {
                name = header.Groups["name"].Value.Trim();
                inParameters = false;
                continue;
            }

            if (Field().Match(trimmed) is { Success: true } field && !line.StartsWith("  "))
            {
                inParameters = false;
                string key = field.Groups["key"].Value.ToLowerInvariant();
                string value = field.Groups["value"].Value.Trim();
                switch (key)
                {
                    case "description":
                        description = value;
                        break;
                    case "timeout":
                        if (!int.TryParse(value, out timeout) || timeout is < 1 or > 600)
                            errors.Add(MessageText.Format(ToolMessage.ScriptTimeoutInvalid, new { Value = value }));
                        break;
                    case "parameters":
                        inParameters = true;
                        break;
                    case "implementation":
                        (language, code, i) = ReadCodeBlock(lines, i + 1, errors);
                        break;
                }
                continue;
            }

            if (inParameters && trimmed.StartsWith('-'))
            {
                if (ParameterLine().Match(trimmed) is { Success: true } p)
                {
                    string type = p.Groups["type"].Value.ToLowerInvariant();
                    if (!SupportedTypes.Contains(type))
                        errors.Add(MessageText.Format(ToolMessage.ScriptParameterTypeUnsupported, new { Name = p.Groups["name"].Value, Type = type, Supported = string.Join(", ", SupportedTypes) }));
                    parameters.Add(new ScriptToolParameter(p.Groups["name"].Value, type,
                        p.Groups["req"].Value.Equals("required", StringComparison.OrdinalIgnoreCase), p.Groups["desc"].Value.Trim()));
                }
                else
                {
                    errors.Add(MessageText.Format(ToolMessage.ScriptParameterLineInvalid, new { Line = trimmed }));
                }
            }
        }

        if (name is null) errors.Add("Missing '# Tool: <PascalCaseName>' header.");
        else if (!ToolName().IsMatch(name)) errors.Add(MessageText.Format(ToolMessage.ScriptToolNameInvalid, new { Name = name }));
        if (string.IsNullOrWhiteSpace(description)) errors.Add("Missing '- Description:' line.");
        if (code is null && !errors.Any(e => e.Contains("Implementation"))) errors.Add(MessageText.Format(ToolMessage.ScriptImplementationMissing));
        foreach (var duplicate in parameters.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add(MessageText.Format(ToolMessage.ScriptParameterDuplicate, new { Name = duplicate.Key }));

        return errors.Count > 0
            ? (null, errors)
            : (new ScriptToolDefinition(name!, description!, parameters, language!, code!, timeout), errors);
    }

    private static (string? Language, string? Code, int LastLine) ReadCodeBlock(string[] lines, int start, List<string> errors)
    {
        int i = start;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
        if (i >= lines.Length || !lines[i].TrimStart().StartsWith("```"))
        {
            errors.Add(MessageText.Format(ToolMessage.ScriptImplementationNotFenced));
            return (null, null, i);
        }

        string fence = lines[i].Trim()[3..].Trim().ToLowerInvariant();
        string language = MarkdownSkillLoader.NormalizeLanguage(fence);
        if (fence.Length > 0 && language == "bash" && fence is not ("bash" or "sh" or "shell"))
            errors.Add(MessageText.Format(ToolMessage.ScriptLanguageUnsupported, new { Language = fence }));

        var body = new List<string>();
        for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```"); i++) body.Add(lines[i]);
        if (i >= lines.Length) errors.Add(MessageText.Format(ToolMessage.ScriptFenceNotClosed));

        string code = string.Join('\n', body).Trim();
        if (code.Length == 0) errors.Add(MessageText.Format(ToolMessage.ScriptCodeEmpty));
        return (language, code, i);
    }

    [GeneratedRegex(@"^#\s*Tool:\s*(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ToolHeader();

    [GeneratedRegex(@"^-\s*(?:\*\*)?(?<key>Description|Timeout|Parameters|Implementation)(?:\*\*)?\s*:\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex Field();

    [GeneratedRegex(@"^-\s*`(?<name>[A-Za-z_][A-Za-z0-9_]*)`\s*\(\s*(?<type>[A-Za-z]+)\s*,\s*(?<req>required|optional)\s*\)\s*:?\s*(?<desc>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParameterLine();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]{2,63}$")]
    private static partial Regex ToolName();
}
