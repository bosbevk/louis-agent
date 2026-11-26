namespace LouisAgent.Core.Skills.Languages;

/// <summary>
/// Which language a script is, by its file's extension (F97.1): <c>.py</c>, <c>.ps1</c> and <c>.sh</c>, the three the
/// runners have. A skill names its script by its path, and its language comes from there, as the Agent Skills spec
/// leaves the languages to whoever runs them.
/// </summary>
/// <remarks>
/// A lookup of its own rather than <c>SettingChoice</c>: an extension isn't the language's name, and only these three
/// files run, so <c>.python</c> or <c>.bash</c> must be refused, not read as a language.
/// </remarks>
/// <seealso href="https://agentskills.io/skill-creation/using-scripts">Agent Skills: using scripts</seealso>
public static class ScriptExtension
{
    private static readonly Dictionary<string, ScriptLanguage> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        [".py"] = ScriptLanguage.Python,
        [".ps1"] = ScriptLanguage.PowerShell,
        [".sh"] = ScriptLanguage.Bash
    };

    /// <summary>The extensions a script may have, for a refusal to list.</summary>
    public static string Known { get; } = string.Join(", ", Languages.Keys);

    /// <summary>The language of the script at this path, when its extension is one we run.</summary>
    public static bool TryLanguageOf(string path, out ScriptLanguage language) =>
        Languages.TryGetValue(Path.GetExtension(path), out language);
}
