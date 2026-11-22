using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Files;

namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// Reads the agent an eval file names into its address, and writes it back as it is written. Text that is no address
/// is refused as a <see cref="JsonException"/> with the reason, so the serializer adds the place in the file.
/// </summary>
internal sealed class AgentAddressConverter : JsonConverter<AgentAddress>
{
    public override AgentAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string text = reader.GetString() ?? "";
        if (AgentAddress.TryParse(text, out var address)) return address;

        var notAnAddress = new NotAnAddressHoles(text);
        throw new JsonException(MessageText.Format(EvalFileMessage.NotAnAddress, notAnAddress));
    }

    public override void Write(Utf8JsonWriter writer, AgentAddress value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
