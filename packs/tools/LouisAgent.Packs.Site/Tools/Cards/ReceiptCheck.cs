using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Messages.Holes.Cards;

namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// What makes a good receipt: a title saying what happened, short enough for one line, and a line on what comes next
/// that fits under it. The first thing wrong, in words the model can act on; nothing when it is good.
/// </summary>
public static class ReceiptCheck
{
    /// <summary>The longest title, one line on the card.</summary>
    public const int TitleLength = 40;

    /// <summary>The longest line under it.</summary>
    public const int LineLength = 120;

    /// <summary>What's wrong with the receipt, or nothing when it is good.</summary>
    public static string? Problem(string title, string line)
    {
        string said = title?.Trim() ?? "";
        if (said.Length == 0) return MessageText.Format(SiteToolMessage.ReceiptWithoutTitle);
        if (said.Length > TitleLength) return TooLong("title", TitleLength, said.Length);

        int next = line?.Trim().Length ?? 0;
        return next > LineLength ? TooLong("line", LineLength, next) : null;
    }

    /// <summary>A part that runs long, named, with the most it may be.</summary>
    private static string TooLong(string part, int most, int length)
    {
        var tooLong = new ReceiptTooLongHoles(part, most, length);
        return MessageText.Format(SiteToolMessage.ReceiptTooLong, tooLong);
    }
}
