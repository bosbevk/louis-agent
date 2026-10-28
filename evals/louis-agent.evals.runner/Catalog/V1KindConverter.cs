using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// Writes a grader's kind in the listing as version 1 did (<c>FixedRule</c>), which the API's listing reader expects
/// until step 7c.3, and reads it back by the member's name or its wire name.
/// </summary>
/// <remarks>
/// A converter on one property, so the listing keeps the kind as the contract's enum while its text stays v1's for
/// now; it goes when the API reads the contract's words.
/// </remarks>
public sealed class V1KindConverter : JsonConverter<GraderKind>
{
    /// <summary>The kind its text names; anything else is not a listing we can read.</summary>
    public override GraderKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        SettingChoice.TryParse(reader.GetString(), out GraderKind? kind) && kind is { } known
            ? known
            : throw new JsonException();

    /// <summary>The kind in version 1's word.</summary>
    public override void Write(Utf8JsonWriter writer, GraderKind value, JsonSerializerOptions options) =>
        writer.WriteStringValue(V1Words.Kind(value));
}
