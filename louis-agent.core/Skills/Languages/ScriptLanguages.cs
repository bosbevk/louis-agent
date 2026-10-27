using LouisAgent.Choices;

namespace LouisAgent.Core.Skills.Languages;

public static class ScriptLanguages
{
    /// <summary>The name a fence, a message and the MCP server write the language as.</summary>
    public static string Name(this ScriptLanguage language) => SettingChoice.Wire(language);
}
