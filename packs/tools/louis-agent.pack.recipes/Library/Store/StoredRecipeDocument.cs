using LouisAgent.Packs.Recipes.Recipes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace LouisAgent.Packs.Recipes.Library.Store;

/// <summary>
/// One recipe as MongoDB keeps it in the <c>kept_recipes</c> collection, field by field, so it can be queried and
/// printed like any other. Its id comes from the page's one spelling, so a page can only be kept once, whoever reads it
/// at the same moment.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the driver
/// does the copying (ADR 0048). The date is a UTC BSON date (ADR 0046).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: Mongo through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
[BsonIgnoreExtraElements]
public sealed record StoredRecipeDocument(
    [property: BsonId] string Id,
    [property: BsonElement("page")] string Page,
    [property: BsonElement("site")] string Site,
    [property: BsonElement("name")] string Name,
    [property: BsonElement("yield")] string? Yield,
    [property: BsonElement("prep_minutes")]
    [property: BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Minutes)]
    TimeSpan? PrepTime,
    [property: BsonElement("cook_minutes")]
    [property: BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Minutes)]
    TimeSpan? CookTime,
    [property: BsonElement("total_minutes")]
    [property: BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Minutes)]
    TimeSpan? TotalTime,
    [property: BsonElement("ingredients")] List<StoredRecipeLine> Ingredients,
    [property: BsonElement("steps")] List<StoredRecipeLine> Steps,
    [property: BsonElement("image")] string? Image,
    [property: BsonElement("author")] string? Author,
    [property: BsonElement("read_at")] DateTime ReadAt,
    [property: BsonElement("edited_at")]
    [property: BsonIgnoreIfNull]
    DateTime? EditedAt = null)
{
    /// <summary>The document for a recipe.</summary>
    public static StoredRecipeDocument Of(StoredRecipe stored) => new(
        stored.Id,
        stored.Page.AbsoluteUri,
        stored.SiteSlug,
        stored.Recipe.Name,
        stored.Recipe.Yield,
        stored.Recipe.PrepTime,
        stored.Recipe.CookTime,
        stored.Recipe.TotalTime,
        [.. stored.Recipe.Ingredients.Select(line => new StoredRecipeLine(line.Section, line.Text))],
        [.. stored.Recipe.Steps.Select(line => new StoredRecipeLine(line.Section, line.Text))],
        stored.Recipe.Image?.AbsoluteUri,
        stored.Recipe.Author,
        stored.ReadAt.UtcDateTime,
        stored.EditedAt?.UtcDateTime);

    /// <summary>The recipe this document holds.</summary>
    public StoredRecipe ToRecipe() => new(
        new Recipe(Name, Yield, PrepTime, CookTime, TotalTime,
            [.. Ingredients.Select(line => new RecipeIngredient(line.Section, line.Text))],
            [.. Steps.Select(line => new RecipeStep(line.Section, line.Text))],
            Image is null ? null : new Uri(Image),
            Author,
            new Uri(Page)),
        Site,
        new DateTimeOffset(DateTime.SpecifyKind(ReadAt, DateTimeKind.Utc)),
        EditedAt is { } edited ? new DateTimeOffset(DateTime.SpecifyKind(edited, DateTimeKind.Utc)) : null);
}

/// <summary>One ingredient or step as MongoDB keeps it, with the part of the recipe it's under.</summary>
public sealed record StoredRecipeLine(
    [property: BsonElement("section")] string? Section,
    [property: BsonElement("text")] string Text);
