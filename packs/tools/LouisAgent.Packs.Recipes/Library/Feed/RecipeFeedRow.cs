using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// One recipe on the open feed, field by field: its own id, the page and the site for the credit, and the recipe as the
/// page gave it. It's both a row of the list and the answer at the recipe's own address. There's no field for who
/// asked.
/// </summary>
/// <remarks>A record because it is a plain value (ADR 0040), and JSON goes through records (ADR 0041).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record RecipeFeedRow(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("page")] string Page,
    [property: JsonPropertyName("site")] string Site,
    [property: JsonPropertyName("site_name")] string SiteName,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("yield")] string? Yield,
    [property: JsonPropertyName("prep_minutes")] double? PrepMinutes,
    [property: JsonPropertyName("cook_minutes")] double? CookMinutes,
    [property: JsonPropertyName("total_minutes")] double? TotalMinutes,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<RecipeFeedLine> Ingredients,
    [property: JsonPropertyName("steps")] IReadOnlyList<RecipeFeedLine> Steps,
    [property: JsonPropertyName("image")] string? Image,
    [property: JsonPropertyName("author")] string? Author,
    [property: JsonPropertyName("read_at")] DateTimeOffset ReadAt)
{
    /// <summary>The row for a recipe, with its site's name for the credit.</summary>
    public static RecipeFeedRow Of(StoredRecipe stored, RecipeSite site) => new(
        stored.Id,
        stored.Page.AbsoluteUri,
        site.Slug,
        site.Name,
        stored.Recipe.Name,
        stored.Recipe.Yield,
        stored.Recipe.PrepTime?.TotalMinutes,
        stored.Recipe.CookTime?.TotalMinutes,
        stored.Recipe.TotalTime?.TotalMinutes,
        [.. stored.Recipe.Ingredients.Select(line => new RecipeFeedLine(line.Section, line.Text))],
        [.. stored.Recipe.Steps.Select(line => new RecipeFeedLine(line.Section, line.Text))],
        stored.Recipe.Image?.AbsoluteUri,
        stored.Recipe.Author,
        stored.ReadAt);

    /// <summary>The recipe this row holds.</summary>
    public StoredRecipe ToRecipe() => new(
        new Recipe(Name, Yield, Time(PrepMinutes), Time(CookMinutes), Time(TotalMinutes),
            [.. Ingredients.Select(line => new RecipeIngredient(line.Section, line.Text))],
            [.. Steps.Select(line => new RecipeStep(line.Section, line.Text))],
            Image is null ? null : new Uri(Image),
            Author,
            new Uri(Page)),
        Site,
        ReadAt);

    /// <summary>The row as its JSON, for the recipe's own address.</summary>
    public string Write() => JsonSerializer.Serialize(this);

    /// <summary>A row from its JSON.</summary>
    public static RecipeFeedRow Read(string json) => JsonSerializer.Deserialize<RecipeFeedRow>(json)!;

    /// <summary>A number of minutes as a time, when the page gave one.</summary>
    private static TimeSpan? Time(double? minutes) => minutes is null ? null : TimeSpan.FromMinutes(minutes.Value);
}

/// <summary>One ingredient or step on the feed, with the part of the recipe it's under.</summary>
public sealed record RecipeFeedLine(
    [property: JsonPropertyName("section")] string? Section,
    [property: JsonPropertyName("text")] string Text);
