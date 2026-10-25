namespace LouisAgent.Core.Tools.Web.Replay.Store;

/// <summary>
/// What a visit asked for, as the replay keys it: the address (<see cref="Uri.AbsoluteUri"/>), the script and the ready selector,
/// compared ordinally. The <c>capture</c> predicate and the cookies a visit starts with are not part of the key and are not saved.
/// </summary>
public sealed record ReplayAsk(string Url, string? Script, string? ReadySelector);
