using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// Writes a time in UTC ending in <c>Z</c> (<c>2026-10-28T08:00:00Z</c>), the way the dates rule, Mongo, JavaScript
/// and Python write it, and reads back a time that ends in <c>Z</c> or carries any offset.
/// </summary>
/// <remarks>
/// A converter on the protocol's one options object (ADR 0041): System.Text.Json writes a UTC time as
/// <c>+00:00</c>, so the one place that writes a time on this wire says how, and no caller formats a time of its own.
/// Reading stays the serializer's own parse, which already takes both forms.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed class UtcZTimeConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// The moment in UTC; the <c>F</c>s drop the zeros a fraction ends with, and its point when none is left.
    /// </summary>
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    /// <summary>A time ending in <c>Z</c> or with an offset, as the serializer reads it.</summary>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset();

    /// <summary>The same moment, in UTC, ending in <c>Z</c>.</summary>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
