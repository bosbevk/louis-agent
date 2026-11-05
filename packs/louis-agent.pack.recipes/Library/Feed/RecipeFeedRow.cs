using System.Text.Json.Serialization;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>One recipe in the feed, with its site's credit and link. It has no place to say who asked.</summary>
/// <remarks>A record because it is a plain value (ADR 0040), and JSON goes through records (ADR 0041).</remarks>
public sealed record RecipeFeedRow(
    [property: JsonPropertyName("page")] string Page,
    [property: JsonPropertyName("site")] string Site,
    [property: JsonPropertyName("site_name")] string SiteName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("print")] string Print,
    [property: JsonPropertyName("read_at")] DateTimeOffset ReadAt)
{
    /// <summary>The row for a kept recipe, on its site.</summary>
    public static RecipeFeedRow Of(StoredRecipe recipe, RecipeSite site) =>
        new(recipe.Page.AbsoluteUri, site.Slug, site.Name, recipe.Title, recipe.Print, recipe.ReadAt);

    /// <summary>The kept recipe this row holds.</summary>
    public StoredRecipe ToRecipe() => new(new Uri(Page), Site, Title, Print, ReadAt);
}
