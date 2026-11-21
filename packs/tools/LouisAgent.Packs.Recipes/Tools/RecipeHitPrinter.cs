using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Providers;
using LouisAgent.Packs.Recipes.Sources.Gathering;

namespace LouisAgent.Packs.Recipes.Tools;

/// <summary>
/// Writes what a search over several sites found for the model: a numbered list that says whose page each hit is, or
/// what to do when there are none. The hits are a site's words, so they go in the frame of untrusted data.
/// </summary>
/// <remarks>Guard clauses, one per way a search can come back empty (ADR 0051).</remarks>
internal static class RecipeHitPrinter
{
    /// <summary>The answer for a search over several sites; the enabled sites can still be named.</summary>
    public static string Print(RecipeGather gather, string query, string enabled)
    {
        if (gather.Asked.Count == 0 && gather.Hits.Count == 0)
            return MessageText.Format(RecipeToolMessage.NoSiteInTheDraw, new { Enabled = enabled });

        // The sites by name, so a next try can skip them, and our library, which a next try asks anyway.
        var names = gather.Asked.Select(site => site.Name);
        string asked = string.Join(", ",
            gather.AskedLibrary ? names.Append(MessageText.Format(RecipeToolMessage.OurLibrary)) : names);
        if (gather.Hits.Count == 0 && gather.Refusals.Count == gather.Asked.Count)
            return MessageText.Format(RecipeToolMessage.EverySiteRefused,
                new
                {
                    Sites = asked,
                    Reasons = string.Join(' ', gather.Refusals)
                });
        if (gather.Hits.Count == 0)
            return string.Join('\n',
            [
                MessageText.Format(RecipeToolMessage.NoSiteHasIt,
                    new
                    {
                        Sites = asked,
                        Query = query
                    }),
                .. OtherNamesLine(QueryWords.OtherNames(query))
            ]);

        // Every site asked, those with no hits too, so a next try can skip them.
        var heading = MessageText.Format(RecipeToolMessage.HitsFoundAcrossSites,
            new
            {
                Sites = asked,
                gather.Hits.Count
            });
        var lines = gather.Hits.Select((hit, index) => MessageText.Format(RecipeToolMessage.HitLineWithSite,
            new
            {
                Number = index + 1,
                Name = HitTitle.Shown(hit.Hit.Name),
                Site = hit.Site.Name,
                Url = hit.Hit.Url.AbsoluteUri
            }));
        return Framed([heading, .. lines, .. OtherNamesLine(QueryWords.AlsoCalled(query))]);
    }

    /// <summary>The line naming what else the dish is called, when there's anything to name.</summary>
    private static string[] OtherNamesLine(IReadOnlyList<string> names) => names.Count > 0
        ? [MessageText.Format(RecipeToolMessage.DishAlsoCalled, new { Names = string.Join(", ", names) })]
        : [];

    /// <summary>The lines between the opening and closing marks of untrusted data.</summary>
    public static string Framed(params string[] lines) => string.Join('\n',
    [
        MessageText.Format(RecipeToolMessage.UntrustedFrame), .. lines, MessageText.Format(RecipeToolMessage.FrameEnd)
    ]);
}
