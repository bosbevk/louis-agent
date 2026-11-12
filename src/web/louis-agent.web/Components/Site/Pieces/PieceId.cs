using System.Text;

namespace LouisAgent.Web.Components.Site.Pieces;

/// <summary>
/// The id of a piece of a page a visitor can select (F127.7), from its title: its letters and digits lower case, every
/// other run of characters one dash, after <c>piece-</c>, so it never clashes with a section's id.
/// </summary>
public static class PieceId
{
    /// <summary>The id from a title: "It answers while you sleep" is <c>piece-it-answers-while-you-sleep</c>.</summary>
    public static string Of(string title)
    {
        var id = new StringBuilder("piece-");
        bool dash = false;
        foreach (char c in title.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                if (dash && id.Length > "piece-".Length) id.Append('-');
                id.Append(char.ToLowerInvariant(c));
                dash = false;
            }
            else
            {
                dash = true;
            }
        }

        return id.ToString();
    }
}
