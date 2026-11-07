using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Screens.Forms;

/// <summary>
/// The kinds of field a form can have, the browser's own copy, since it can't reference the packs' contract;
/// <c>BrowserSideNamesTests</c> checks the two name for name.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ScreenFieldKind
{
    /// <summary>Plain words.</summary>
    [WireName("text")] Text,

    /// <summary>A number.</summary>
    [WireName("number")] Number,

    /// <summary>A web address.</summary>
    [WireName("link")] Link,

    /// <summary>A list, a row per line.</summary>
    [WireName("rows")] Rows
}

/// <summary>What a field's kind means for the page.</summary>
public static class ScreenFieldKinds
{
    /// <summary>A field's kind. A name this browser doesn't know is edited as plain words.</summary>
    public static ScreenFieldKind Of(string wireName) =>
        SettingChoice.TryParse<ScreenFieldKind>(wireName, out var kind) ? kind!.Value : ScreenFieldKind.Text;

    /// <summary>The input a kind is typed into.</summary>
    public static string InputType(ScreenFieldKind kind) => kind switch
    {
        ScreenFieldKind.Number => "number",
        ScreenFieldKind.Link => "url",
        _ => "text"
    };
}
