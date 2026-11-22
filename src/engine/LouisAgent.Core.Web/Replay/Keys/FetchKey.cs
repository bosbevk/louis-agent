namespace LouisAgent.Core.Web.Replay.Keys;

/// <summary>What a fetch is recorded and replayed by: its method and address.</summary>
/// <remarks>
/// Serialized as it stands into the key a recording is found by, so its properties keep their names and order: a
/// recorded run is replayed from them.
/// </remarks>
public sealed record FetchKey(string Method, string Url);
