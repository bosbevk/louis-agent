using System.Text;

namespace LouisAgent.Web.Components.Site.Pieces;

/// <summary>
/// The id of a piece of a page a visitor or the guide can select (F127.7), from its title: its letters and digits
/// lower case, every other run of characters one dash, so its address reads like a card's (ADR 0107):
/// <c>/home/it-answers-while-you-sleep</c>.
/// </summary>
public static class PieceId
{
    /// <summary>The id from a title: "It answers while you sleep" is <c>it-answers-while-you-sleep</c>.</summary>
    public static string Of(string title)
    {
        var id = new StringBuilder();
        bool dash = false;
        foreach (char c in title.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                if (dash && id.Length > 0) id.Append('-');
                id.Append(char.ToLowerInvariant(c));
                dash = false;
            }
            else
            {
                dash = true;
            }
        }

        // A number alone would read as a page of a paged section, so it takes a word.
        string made = id.ToString();
        return made.Length > 0 && made.All(char.IsAsciiDigit) ? $"number-{made}" : made;
    }
}
