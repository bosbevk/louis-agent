using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Turns our single values into Mongo's and back: times, numbers that may be missing, stored names and raw JSON. Each
/// document mapper uses them, so a missing value is written and read the same way in every collection.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
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

    /// <summary>A whole number, or Mongo's null.</summary>
    public static BsonValue Nullable(int? value) => value is null ? BsonNull.Value : new BsonInt32(value.Value);

    /// <summary>A decimal number, or Mongo's null.</summary>
    public static BsonValue Nullable(double? value) => value is null ? BsonNull.Value : new BsonDouble(value.Value);

    /// <summary>A big whole number, or Mongo's null.</summary>
    public static BsonValue Nullable(long? value) => value is null ? BsonNull.Value : new BsonInt64(value.Value);

    /// <summary>A money-sized number, or Mongo's null.</summary>
    public static BsonValue Nullable(decimal? value) =>
        value is null ? BsonNull.Value : new BsonDecimal128(value.Value);

    /// <summary>
    /// A stored name turned back into its enum, the way it was parsed on the way in. A name this build doesn't know
    /// means a document it can't read, so it says which field and which name.
    /// </summary>
    public static T ChoiceOf<T>(BsonValue value, string field) where T : struct, Enum =>
        SettingChoice.TryParse(value.AsString, out T? choice) && choice is { } known
            ? known
            : throw new InvalidDataException(MessageText.Format(ApiUserText.StoredNameUnknown,
                new
                {
                    Field = field,
                    Value = value.AsString
                }));

    /// <summary>The stored text, if there is one.</summary>
    public static string? StringOf(BsonValue value) => value.IsBsonNull ? null : value.AsString;

    /// <summary>The stored whole number, if there is one.</summary>
    public static long? LongOf(BsonValue value) => value.IsBsonNull ? null : value.ToInt64();

    /// <summary>The stored decimal number, if there is one.</summary>
    public static double? DoubleOf(BsonValue value) => value.IsBsonNull ? null : value.ToDouble();

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
