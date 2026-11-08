using System.Text;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// Turns a title into an id a link and the guide can name (F118): "The coding agent" becomes <c>the-coding-agent</c>,
/// "louis-agent.pack.files" becomes <c>louis-agent-pack-files</c>. So a card is named by its title without a site
/// writing an id, as a band is named because the framework asks for one.
/// </summary>
public static class AreaIds
{
    /// <summary>
    /// Lowercase letters and digits as they are, every other run of characters one dash, no dash at either end; an id
    /// starts with a letter, so a title that opens with a digit gets an <c>a-</c> in front.
    /// </summary>
    public static string FromWords(string words)
    {
        var id = new StringBuilder(words.Length);
        foreach (char c in words.ToLowerInvariant())
        {
            // A letter or digit is kept; anything else becomes one dash, never two in a row.
            if (char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c)) id.Append(c);
            else if (id.Length > 0 && id[^1] != '-') id.Append('-');
        }

        string trimmed = id.ToString().Trim('-');
        if (trimmed.Length == 0) return "area";
        return char.IsAsciiLetterLower(trimmed[0]) ? trimmed : $"a-{trimmed}";
    }
}
