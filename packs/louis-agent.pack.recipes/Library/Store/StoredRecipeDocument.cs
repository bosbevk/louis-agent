using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Packs.Recipes.Library.Store;

/// <summary>
/// One recipe as MongoDB keeps it in the <c>kept_recipes</c> collection. Its id is the page's one spelling, so a page
/// can only be kept once, whoever reads it at the same moment.
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
    [property: BsonElement("title")] string Title,
    [property: BsonElement("print")] string Print,
    [property: BsonElement("read_at")] DateTime ReadAt)
{
    /// <summary>The document for a recipe.</summary>
    public static StoredRecipeDocument Of(StoredRecipe recipe) => new(
        RecipeAddress.Of(recipe.Page),
        recipe.Page.AbsoluteUri,
        recipe.SiteSlug,
        recipe.Title,
        recipe.Print,
        recipe.ReadAt.UtcDateTime);

    /// <summary>The recipe this document holds.</summary>
    public StoredRecipe ToRecipe() =>
        new(new Uri(Page), Site, Title, Print, new DateTimeOffset(DateTime.SpecifyKind(ReadAt, DateTimeKind.Utc)));
}
