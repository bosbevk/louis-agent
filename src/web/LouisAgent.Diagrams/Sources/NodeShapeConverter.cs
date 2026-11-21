using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Reading;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// Reads a box's shape by its name, and refuses a name that is no shape with the shapes there are (F134.8), where
/// the serializer on its own would only say the value can't be read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal sealed class NodeShapeConverter : JsonConverter<NodeShape>
{
    /// <summary>The shape a file names. A number or anything else that is no name is refused the same way.</summary>
    public override NodeShape Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        // Nothing written is no shape either: a box that wants none leaves the field out.
        if (SettingChoice.TryParse<NodeShape>(name, out var shape) && shape is { } known) return known;

        var unknown = new UnknownShapeHoles(name, string.Join(", ", SettingChoice.Names<NodeShape>()));
        throw new InvalidDataException(DiagramText.Format(DiagramMessage.UnknownShape, unknown));
    }

    /// <summary>A shape is written by its name.</summary>
    public override void Write(Utf8JsonWriter writer, NodeShape value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Wire());
}
