using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Types;

namespace LouisAgent.Diagrams.Sources.Types;

/// <summary>
/// Reads a picture's type by its name, and refuses a name that is no type of diagram with the types there are
/// (CL65.3), where the serializer on its own would only say the value can't be read. A type's name has a hyphen
/// where UML's has a space, so the serializer's own enum names would not read it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal sealed class DiagramTypeConverter : JsonConverter<DiagramType>
{
    /// <summary>The type a file names. A number or anything else that is no name is refused the same way.</summary>
    public override DiagramType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (SettingChoice.TryParse<DiagramType>(name, out var type) && type is { } known) return known;

        var unknown = new UnknownDiagramTypeHoles(name, string.Join(", ", SettingChoice.Names<DiagramType>()));
        throw new InvalidDataException(DiagramText.Format(DiagramMessage.UnknownDiagramType, unknown));
    }

    /// <summary>A type is written by its name.</summary>
    public override void Write(Utf8JsonWriter writer, DiagramType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Wire());
}
