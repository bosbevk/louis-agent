using System.Globalization;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>
/// One UTC day's numbers as MongoDB keeps them. The day is the id, written <c>2026-11-06</c>, so a day has one record
/// and the ids sort by date. A number nothing has added to yet isn't in the document, and reads as nothing.
/// </summary>
/// <remarks>Metadata Mapping on a Data Transfer Object (Fowler, PoEAA), through a typed record (ADR 0048).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: Mongo through records</seealso>
[BsonIgnoreExtraElements]
public sealed record RecipeUsageDocument(
    [property: BsonId] string Id,
    [property: BsonElement("kept"), BsonDefaultValue(0L)] long Kept,
    [property: BsonElement("offered"), BsonDefaultValue(0L)] long Offered,
    [property: BsonElement("served"), BsonDefaultValue(0L)] long Served,
    [property: BsonElement("failed"), BsonDefaultValue(0L)] long Failed)
{
    /// <summary>The id of a day's record.</summary>
    public static string IdOf(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>The day this record holds.</summary>
    public RecipeUsageDay ToDay() =>
        new(DateOnly.ParseExact(Id, "yyyy-MM-dd", CultureInfo.InvariantCulture), Kept, Offered, Served, Failed);
}
