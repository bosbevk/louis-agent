using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace LouisAgent.Api.Evals.Store.Mongo;

/// <summary>
/// Stores an enum in Mongo as its wire name (<c>fixed-rule</c>), never its C# name or a number, and reads it back
/// from that name. A stored value that names no member, a number among them, is refused and says what it found.
/// </summary>
/// <remarks>
/// A serializer the driver uses for every property of this enum, registered once (ADR 0048), so a document record
/// needs no attribute per field and the stored text is the same <c>.Wire()</c> the protocol and the replies use. The
/// driver's own string representation would store the C# name, which is not the wire name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed class WireNameSerializer<TEnum> : StructSerializerBase<TEnum> where TEnum : struct, Enum
{
    /// <summary>Writes the member's wire name.</summary>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value) =>
        context.Writer.WriteString(value.Wire());

    /// <summary>Reads a stored wire name back as its member; anything else is refused with what was stored.</summary>
    public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        // A number or anything but text names no member; it's read whole, so the error can say what it was.
        var stored = BsonValueSerializer.Instance.Deserialize(context);
        if (stored.IsString && SettingChoice.TryParse(stored.AsString, out TEnum? member) && member is { } known)
            return known;

        throw new InvalidDataException(MessageText.Format(ApiUserText.StoredNameUnknown,
            new
            {
                Field = typeof(TEnum).Name,
                Value = stored.ToString()
            }));
    }
}
