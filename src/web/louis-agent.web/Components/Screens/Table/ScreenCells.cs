using System.Globalization;
using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Screens.Table;

/// <summary>What a cell needs to know before it's drawn: its kind, and how its value reads.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public static class ScreenCells
{
    /// <summary>A column's kind. A name this browser doesn't know draws as plain words.</summary>
    public static ScreenCellKind KindOf(string wireName) =>
        SettingChoice.TryParse<ScreenCellKind>(wireName, out var kind) ? kind!.Value : ScreenCellKind.Text;

    /// <summary>
    /// Only a web address may become a link. These values can come from pages we read, and a <c>javascript:</c> address
    /// would run in the admin's tab, with its key.
    /// </summary>
    public static bool IsWebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var address) && address.Scheme is "http" or "https";

    /// <summary>How a value reads: a date in the reader's own time, anything else as it came.</summary>
    public static string Show(ScreenCellKind kind, string value)
    {
        if (kind is not ScreenCellKind.Date)
            return value;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? at.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : value;
    }
}
