using System.Text.Json;

namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>
/// What a seam was asked, as the replay keys it: the seam (<see cref="ReplayKind"/>) and the key, the ask as compact JSON built from an
/// anonymous object by <see cref="Of"/> and compared ordinally. The key's fields are part of the recording format: changing them makes
/// earlier recordings miss, which is fine. Enums in a key are written as names, never numbers (ADR 0024). Anything not in the key (a visit's
/// <c>capture</c> predicate and cookies, a request's headers) is not saved.
/// </summary>
public sealed record ReplayAsk(ReplayKind Kind, string Key)
{
    public static ReplayAsk Of(ReplayKind kind, object key) => new(kind, JsonSerializer.Serialize(key, ReplayStore.Json));
}
