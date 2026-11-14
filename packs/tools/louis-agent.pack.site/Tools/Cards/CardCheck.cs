using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;

namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// What makes a card the widget can draw: a title, one to six rows each with its label and value, icons and a tone
/// the diagrams library has. The first thing wrong, in words the model can act on; nothing when the card is whole.
/// </summary>
/// <remarks>
/// A tested duplicate of the library's own rules (the engine-split rule: a shared contract is a shared project or a
/// tested duplicate): a pack references no web (ADR 0086), and the library lives with the web. The pack's tests hold
/// these names and rules equal to the library's, so a card this calls whole is a card the widget draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public static class CardCheck
{
    /// <summary>The icons the library draws, by name.</summary>
    public static IReadOnlyList<string> Icons { get; } =
    [
        "chip", "user", "users", "box", "server", "database", "world", "message", "shield", "code", "folder", "file",
        "browser", "checks", "chart", "cloud", "settings", "branch", "mail", "search", "book", "terminal", "stack",
        "info", "calendar", "clock", "phone", "home", "tag", "paw", "bed", "dining", "scales", "exchange",
        "chevron-left", "chevron-right"
    ];

    /// <summary>The tones a status may wear, by name.</summary>
    public static IReadOnlyList<string> Tones { get; } = ["plain", "blue", "green", "violet", "teal", "red", "amber"];

    /// <summary>What's wrong with the card, or nothing when it would draw.</summary>
    public static string? Problem(string title, IReadOnlyList<CardRow> rows, CardStatus? status)
    {
        if (string.IsNullOrWhiteSpace(title)) return MessageText.Format(SiteToolMessage.CardWithoutTitle, new { });
        if (rows.Count is < 1 or > 6) return MessageText.Format(SiteToolMessage.CardRowCount, new { rows.Count });

        for (int at = 0; at < rows.Count; at++)
        {
            if (string.IsNullOrWhiteSpace(rows[at].Label) || string.IsNullOrWhiteSpace(rows[at].Value))
                return MessageText.Format(SiteToolMessage.CardHalfRow, new { Number = at + 1 });
            if (!Named(Icons, rows[at].Icon))
                return MessageText.Format(SiteToolMessage.CardUnknownIcon,
                    new
                    {
                        Number = at + 1,
                        rows[at].Icon,
                        Icons = string.Join(", ", Icons)
                    });
        }

        if (status?.Tone is not { } tone || Named(Tones, tone)) return null;
        return MessageText.Format(SiteToolMessage.CardUnknownTone,
            new
            {
                Tone = tone,
                Tones = string.Join(", ", Tones)
            });
    }

    /// <summary>Whether a name is one of these, its case ignored as the library ignores it.</summary>
    private static bool Named(IReadOnlyList<string> names, string? name) =>
        names.Contains(name?.Trim(), StringComparer.OrdinalIgnoreCase);
}
