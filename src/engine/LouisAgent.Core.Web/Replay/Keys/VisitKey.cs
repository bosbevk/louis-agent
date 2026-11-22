namespace LouisAgent.Core.Web.Replay.Keys;

/// <summary>What a browser's visit is recorded and replayed by: the page, the script it ran and what it waited
/// for.</summary>
/// <remarks>
/// Serialized as it stands into the key a recording is found by, so its properties keep their names and order: a
/// recorded run is replayed from them.
/// </remarks>
public sealed record VisitKey(string Url, string? Script, string? ReadySelector);
