using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Config.Messages;
using LouisAgent.Core.Config.Messages.Holes;

namespace LouisAgent.Core.Config.ToolSearch;

/// <summary>Reads TOOL_SEARCH once, where it enters: unset is auto, and a word it doesn't know stops start-up.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class ToolSearchSetting
{
    /// <summary>The setting's name, as a host's settings and the error say it.</summary>
    public const string Name = "TOOL_SEARCH";

    /// <summary>The mode the setting names, or auto when it names none.</summary>
    public static ToolSearchMode Parse(string? setting)
    {
        if (!SettingChoice.TryParse(setting, out ToolSearchMode? mode))
        {
            var unknown = new UnknownToolSearchHoles(Name, setting?.Trim() ?? "", Modes());
            throw new ArgumentException(MessageText.Format(ConfigErrorMessage.UnknownToolSearch, unknown));
        }

        return mode ?? ToolSearchMode.Auto;
    }

    /// <summary>The modes by their wire names, as a person types them: "auto, on or off".</summary>
    private static string Modes()
    {
        var names = Enum.GetValues<ToolSearchMode>().Select(mode => mode.Wire()).ToList();
        return $"{string.Join(", ", names[..^1])} or {names[^1]}";
    }
}
