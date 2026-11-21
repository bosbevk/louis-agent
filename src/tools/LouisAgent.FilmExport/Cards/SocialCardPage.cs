using LouisAgent.Choices;

namespace LouisAgent.FilmExport.Cards;

/// <summary>
/// The share card's page (FX174, Louis: "where is my glass?"): its template, from the file beside this one, with the
/// photograph of the site's own glass orb in each place the orb stands.
/// </summary>
public static class SocialCardPage
{
    /// <summary>Where the orb's photograph goes in the template.</summary>
    private const string OrbHole = "{orb}";

    /// <summary>The template's file, from the repo's root.</summary>
    public const string TemplatePath = "src/tools/LouisAgent.FilmExport/Cards/social-card.html";

    /// <summary>The card's size in points, the size and shape Facebook and LinkedIn ask a link preview for.</summary>
    public const int Width = 1200, Height = 630;

    /// <summary>Where the card's look goes in the template, as the class its page reads.</summary>
    private const string LookHole = "{look}";

    /// <summary>The template with the orb's photograph, as a data address, in each place for it, in this look.</summary>
    public static string Fill(string template, byte[] orb, CardLook look) =>
        template.Replace(OrbHole, $"data:image/png;base64,{Convert.ToBase64String(orb)}", StringComparison.Ordinal)
            .Replace(LookHole, look.Wire(), StringComparison.Ordinal);
}
