using LouisAgent.Packs.ShopPrices.Prices.Files;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>
/// One price table as MongoDB keeps it in the <c>price_files</c> collection: one document per source and month, so
/// every month stays as history and the newest one is read. Its id is the source and the date, so a month is saved
/// once.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the driver
/// does the copying; the products inside are the pack's own records, which carry their field names too (ADR 0048).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
public sealed record PriceFileDocument(
    [property: BsonId] string Id,
    [property: BsonElement("source")] string Source,
    [property: BsonElement("as_of")] DateOnly AsOf,
    [property: BsonElement("products")] IReadOnlyList<PriceProduct> Products)
{
    /// <summary>The document for a table: its id is the month and the source.</summary>
    public static PriceFileDocument Of(PriceFile file) =>
        new($"{file.AsOf:yyyy-MM-dd} {file.Source}", file.Source, file.AsOf, file.Products);

    /// <summary>The table this document holds.</summary>
    public PriceFile ToFile() => new(Source, AsOf, Products);
}
