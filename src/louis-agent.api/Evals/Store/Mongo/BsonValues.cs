using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Turns our single values into Mongo's and back for the hand-written mappers still left (the turns' and the
/// requests'): times, numbers that may be missing, and raw JSON, so a missing value is written and read the same way
/// in each. It shrinks as each collection moves to its document record.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal static class BsonValues
{
    /// <summary>How BSON is written back out as JSON.</summary>
    private static readonly JsonWriterSettings RelaxedJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <summary>A time as a Mongo date in UTC; nothing stays nothing.</summary>
    public static BsonValue Date(DateTimeOffset? at) =>
        at is { } value ? new BsonDateTime(value.UtcDateTime) : BsonNull.Value;

    /// <summary>A Mongo date turned back into a time.</summary>
    public static DateTimeOffset? DateOf(BsonValue value) =>
        value.IsBsonNull ? null : new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero);

    /// <summary>A text, or Mongo's null when there is none. The same for the numbers below.</summary>
    public static BsonValue Nullable(string? value) => value is null ? BsonNull.Value : new BsonString(value);

    /// <summary>A big whole number, or Mongo's null.</summary>
    public static BsonValue Nullable(long? value) => value is null ? BsonNull.Value : new BsonInt64(value.Value);

    /// <summary>A money-sized number, or Mongo's null.</summary>
    public static BsonValue Nullable(decimal? value) =>
        value is null ? BsonNull.Value : new BsonDecimal128(value.Value);

    /// <summary>The stored text, if there is one.</summary>
    public static string? StringOf(BsonValue value) => value.IsBsonNull ? null : value.AsString;

    /// <summary>The stored whole number, if there is one.</summary>
    public static long? LongOf(BsonValue value) => value.IsBsonNull ? null : value.ToInt64();

    /// <summary>The stored money-sized number, if there is one.</summary>
    public static decimal? DecimalOf(BsonValue value) => value.IsBsonNull ? null : value.ToDecimal();

    /// <summary>
    /// Any JSON value as BSON. It's wrapped in an object and unwrapped again, because BSON's root must be a document.
    /// </summary>
    public static BsonValue Bson(JsonElement json) => BsonDocument.Parse($$"""{"v":{{json.GetRawText()}}}""")["v"];

    /// <summary>A BSON value turned back into the JSON it came as.</summary>
    public static JsonElement Json(BsonValue bson)
    {
        // Wrapped the same way as on the way in, then the wrapper is read off again.
        using var document = JsonDocument.Parse(new BsonDocument("v", bson).ToJson(RelaxedJson));
        return document.RootElement.GetProperty("v").Clone();
    }
}
