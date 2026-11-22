using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// Reads a fact from its words in the file, and writes it back as them. Words that are no fact are refused as a
/// <see cref="JsonException"/> with the reason, so the serializer adds the place in the file.
/// </summary>
internal sealed class FactConverter : JsonConverter<Fact>
{
    public override Fact Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string words = reader.GetString() ?? "";
        try
        {
            return Fact.Parse(words);
        }
        catch (FormatException refused)
        {
            throw new JsonException(refused.Message);
        }
    }

    public override void Write(Utf8JsonWriter writer, Fact value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
