using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Core.Web.Replay.Keys;

/// <summary>What a search is recorded and replayed by: who ran it and what it asked.</summary>
/// <remarks>
/// Serialized as it stands into the key a recording is found by, so its properties keep their names and order: a
/// recorded run is replayed from them.
/// </remarks>
public sealed record SearchKey(string Provider, string Text, int MaxResults, string? Site, SearchRecency? Recency);
