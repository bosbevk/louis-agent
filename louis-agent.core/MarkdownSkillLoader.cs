using System.Text;
using System.Text.RegularExpressions;

namespace louis_agent.core;

public class MarkdownSkillLoader
{
    /// <param name="Language">Code-fence language of the execution block: bash (default), powershell or python.</param>
    public record SkillDef(string Name, string Description, string Command, string Language = "bash");

    /// <summary>Maps a code-fence tag to a supported skill language; unknown or empty tags run as bash.</summary>
    public static string NormalizeLanguage(string? fenceTag) => (fenceTag ?? "").Trim().ToLowerInvariant() switch
    {
        "powershell" or "pwsh" or "ps1" or "ps" => "powershell",
        "python" or "py" or "python3" => "python",
        _ => "bash",
    };

    public static List<SkillDef> Load(string filePath)
    {
        var skills = new List<SkillDef>();
        if (!File.Exists(filePath)) return skills;

        var lines = File.ReadAllLines(filePath);
        string? name = null;
        string? description = null;
        StringBuilder? commandBuilder = null;
        string language = "bash";

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
                language = "bash";
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
                        language = "bash";
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