using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Screens.Table;

/// <summary>
/// The kinds of column a screen can have, the browser's own copy, since it can't reference the packs' contract;
/// <c>BrowserSideNamesTests</c> checks the two name for name.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ScreenCellKind
{
    /// <summary>Plain words.</summary>
    [WireName("text")] Text,

    /// <summary>A number, already written as it should read, lined up on the right.</summary>
    [WireName("number")] Number,

    /// <summary>A web address, which opens in its own tab.</summary>
    [WireName("link")] Link,

    /// <summary>A UTC date, shown in the reader's own time.</summary>
    [WireName("date")] Date
}
