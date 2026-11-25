using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Choices;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Usage.Records;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Sessions.Store.Mongo;

/// <summary>
/// How a chat opened, as the <c>sessions</c> collection keeps it (F43.1): the request's fields one by one, the stream's
/// format by its wire name, and the chat's last activity, which its TTL index reads.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
internal sealed record SessionDocument(
    [property: BsonId] string Id,
    [property: BsonElement("toolsets")] List<string>? Toolsets,
    [property: BsonElement("depth")] int Depth,
    [property: BsonElement("role")] string? Role,
    [property: BsonElement("location")] string? Location,
    [property: BsonElement("time_zone")] string? TimeZone,
    [property: BsonElement("llm")] string? Llm,
    [property: BsonElement("tool")] string? Tool,
    [property: BsonElement("format")] string Format,
    [property: BsonElement("task")] string? Task,
    [property: BsonElement("run")] string? Run,
    [property: BsonElement("service")] string? Service,
    [property: BsonElement("parent")] string? Parent,
    [property: BsonElement("opener_user")] string? OpenerUser,
    [property: BsonElement("opener_tenant")] string? OpenerTenant,
    [property: BsonElement("identity")] string? Identity,
    [property: BsonElement("admitted_as")] string? AdmittedAs,
    [property: BsonElement("opened_at"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)] DateTime OpenedAt,
    [property: BsonElement("last_activity"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)] DateTime LastActivity)
{
    /// <summary>The document for a chat as it opens: its last activity is its opening.</summary>
    public static SessionDocument Of(StoredSession session)
    {
        var request = session.Opening;
        var tags = request.Tags ?? UsageTags.None;
        return new SessionDocument(session.Id, request.Toolsets?.ToList(), request.Depth, request.Role,
            request.Place?.Location, request.Place?.TimeZone, request.Llm, request.Tool, request.Format.Wire(),
            tags.Task, tags.Run, tags.Service, tags.Parent, request.Opener?.UserId, request.Opener?.TenantId,
            request.Identity, session.AdmittedAs, session.OpenedAt.UtcDateTime, session.OpenedAt.UtcDateTime);
    }

    /// <summary>The chat as the store hands it back: its request rebuilt as it was asked for.</summary>
    public StoredSession ToStored()
    {
        SettingChoice.TryParse(Format, out StreamFormat? format);
        var place = Location is null && TimeZone is null ? null : new UserPlace(Location, TimeZone);
        var opener = OpenerUser is null ? null : new SessionOpener(OpenerUser, OpenerTenant ?? string.Empty);
        var request = new SessionRequest(Toolsets, new UsageTags(Task, Run, Service, Parent), Depth, Role, place, Llm,
            Tool, format ?? StreamFormat.Claude, null, opener, Identity);
        return new StoredSession(Id, request, new DateTimeOffset(OpenedAt, TimeSpan.Zero), AdmittedAs);
    }
}
