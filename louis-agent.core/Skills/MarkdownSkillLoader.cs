using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Choices;

namespace LouisAgent.Core.Skills;

public class MarkdownSkillLoader
{
    /// <param name="Language">Code-fence language of the execution block: bash (default), powershell or python.</param>
    public record SkillDef(string Name, string Description, string Command, ScriptLanguage Language = ScriptLanguage.Bash);

    /// <summary>Maps a code-fence tag to a supported skill language; unknown or empty tags run as bash.</summary>
    public static ScriptLanguage NormalizeLanguage(string? fenceTag) =>
        SettingChoice.TryParse(fenceTag, out ScriptLanguage? language, FenceAliases) && language is { } known ? known : ScriptLanguage.Bash;

    /// <summary>Whether a fence tag names a language (or is the shell's own `sh` / `shell`); any other tag runs as bash, but a tool definition refuses it.</summary>
    public static bool IsKnownFence(string? fenceTag) =>
        (fenceTag ?? "").Trim().ToLowerInvariant() is "sh" or "shell" || SettingChoice.TryParse(fenceTag, out ScriptLanguage? _, FenceAliases);

    /// <summary>The other tags a fence is written with (the language's own name and wire name already match).</summary>
    private static readonly Dictionary<string, ScriptLanguage> FenceAliases = new()
    {
        ["pwsh"] = ScriptLanguage.PowerShell, ["ps1"] = ScriptLanguage.PowerShell, ["ps"] = ScriptLanguage.PowerShell,
        ["py"] = ScriptLanguage.Python, ["python3"] = ScriptLanguage.Python,
    };

    public static List<SkillDef> Load(string filePath) => File.Exists(filePath) ? Parse(File.ReadAllLines(filePath)) : [];

    /// <summary>Parses skill text already in memory, such as a built-in file embedded in the assembly (F5 step 4b).</summary>
    public static List<SkillDef> Parse(string text) => Parse(text.ReplaceLineEndings("\n").Split('\n'));

    private static List<SkillDef> Parse(string[] lines)
    {
        var skills = new List<SkillDef>();
        string? name = null;
        string? description = null;
        StringBuilder? commandBuilder = null;
        var language = ScriptLanguage.Bash;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var skillHeader = Regex.Match(line, @"^##\s*Skill:\s*(?<name>.+?)\s*$");

            if (skillHeader.Success)
            {
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(description) && commandBuilder is not null)
                {
                    skills.Add(new SkillDef(name, description, commandBuilder.ToString().Trim(), language));
                }

                name = skillHeader.Groups["name"].Value.Trim();
                description = null;
                commandBuilder = null;
                language = ScriptLanguage.Bash;
                continue;
            }

            if (string.IsNullOrWhiteSpace(name)) continue;

            var descriptionMatch = Regex.Match(line, @"^-\s*(?:\*\*)?Description(?:\*\*)?\s*:\s*(?<desc>.*)$");
            if (descriptionMatch.Success)
            {
                description = descriptionMatch.Groups["desc"].Value.Trim();
                continue;
            }

            var executionMatch = Regex.Match(line, @"^-\s*(?:\*\*)?Execution(?:\*\*)?\s*:\s*(?<exec>.*)$");
            if (executionMatch.Success)
            {
                var executionText = executionMatch.Groups["exec"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(executionText))
                {
                    if (executionText.StartsWith("`") && executionText.EndsWith("`"))
                    {
                        commandBuilder = new StringBuilder(executionText.Trim('`'));
                        language = ScriptLanguage.Bash;
                        continue;
                    }
                }

                var nextIndex = i + 1;
                while (nextIndex < lines.Length && string.IsNullOrWhiteSpace(lines[nextIndex]))
                {
                    nextIndex++;
                }

                if (nextIndex < lines.Length)
                {
                    var nextLine = lines[nextIndex].Trim();
                    if (nextLine.StartsWith("```"))
                    {
                        commandBuilder = new StringBuilder();
                        language = NormalizeLanguage(nextLine[3..]);
                        nextIndex++;
                        while (nextIndex < lines.Length && !lines[nextIndex].Trim().StartsWith("```"))
                        {
                            commandBuilder.AppendLine(lines[nextIndex]);
                            nextIndex++;
                        }

                        i = nextIndex;
                        continue;
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(description) && commandBuilder is not null)
        {
            skills.Add(new SkillDef(name, description, commandBuilder.ToString().Trim(), language));
        }

        return skills;
    }
}