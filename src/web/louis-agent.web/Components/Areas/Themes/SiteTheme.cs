using LouisAgent.Composition.Messages;
using LouisAgent.Web.Messages;
using LouisAgent.Web.Messages.Holes;

namespace LouisAgent.Web.Components.Areas.Themes;

/// <summary>
/// One theme a site can wear (F151.1): its id, written into the page's <c>data-theme</c> and the selector of its
/// stylesheet's blocks, and the name a person reads on the switch. A theme is only values, so this is all the C#
/// side knows of one; its look is its package's stylesheet. The id is checked when the theme is made, since the
/// browser keeps it and the page writes it back into an attribute and a selector: lower-case letters, digits and
/// hyphens only, so nothing in it can break out of either.
/// </summary>
/// <param name="Id">What the page and the stylesheet call the theme, such as <c>glass</c>.</param>
/// <param name="Name">What the switch shows a person, such as <c>Glass</c>.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public sealed record SiteTheme(string Id, string Name)
{
    /// <summary>What the page and the stylesheet call the theme, refused unless it can go there as it is.</summary>
    public string Id { get; } = Checked(Id);

    /// <summary>The id as it was given, or the refusal with the id in it.</summary>
    private static string Checked(string id)
    {
        if (id.Length > 0 && id.All(IsIdCharacter)) return id;

        var refused = new ThemeIdRefusedHoles(id);
        throw new ArgumentException(MessageText.Format(WebErrorMessage.ThemeIdRefused, refused), nameof(id));
    }

    /// <summary>A lower-case letter, a digit or a hyphen.</summary>
    private static bool IsIdCharacter(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
}
