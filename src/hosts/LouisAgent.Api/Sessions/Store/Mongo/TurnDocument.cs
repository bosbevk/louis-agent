using System.Text.Json;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Store.Mongo;

/// <summary>
/// One finished turn, as the <c>turns</c> collection keeps it (F43.1): its chat, its number, when, its messages as
/// Microsoft.Extensions.AI's own JSON (so a thinking block's signature survives, ADR 0008), its text attachments, and
/// its chat's last activity, which the TTL index reads so a chat's turns go with it.
/// </summary>
/// <remarks>
/// The messages are a JSON string, not BSON fields: a message's contents take many shapes, and the JSON the model's
/// recordings are already written in reads every one of them back. The attachments are a list of pairs, since a file's
/// name has a dot, which a BSON field name may not.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
internal sealed record TurnDocument(
    [property: BsonId] string Id,
    [property: BsonElement("session_id")] string SessionId,
    [property: BsonElement("number")] int Number,
    [property: BsonElement("at"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)] DateTime At,
    [property: BsonElement("messages")] string Messages,
    [property: BsonElement("attachments"), BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
    Dictionary<string, string> Attachments,
    [property: BsonElement("last_activity"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)] DateTime LastActivity)
{
    /// <summary>
    /// The document for a chat's finished turn, its id the chat's and its number, so it is written once.
    /// </summary>
    public static TurnDocument Of(string sessionId, StoredTurn turn) =>
        new($"{sessionId}:{turn.Number}", sessionId, turn.Number, turn.At.UtcDateTime,
            JsonSerializer.Serialize(turn.Messages, AIJsonUtilities.DefaultOptions),
            turn.Attachments.ToDictionary(), turn.At.UtcDateTime);

    /// <summary>The turn as the store hands it back.</summary>
    public StoredTurn ToStored() =>
        new(Number, new DateTimeOffset(At, TimeSpan.Zero),
            JsonSerializer.Deserialize<List<ChatMessage>>(Messages, AIJsonUtilities.DefaultOptions) ?? [],
            Attachments);
}
