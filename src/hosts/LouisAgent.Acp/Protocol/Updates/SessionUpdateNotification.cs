namespace LouisAgent.Acp.Protocol.Updates;

/// <summary>
/// What a <c>session/update</c> notification carries: which chat it is for, and what happened in it.
/// </summary>
/// <param name="SessionId">The chat the update belongs to.</param>
/// <param name="Update">What happened; typed as the base so its kind is written beside it.</param>
internal sealed record SessionUpdateNotification(string SessionId, SessionUpdate Update)
{
    /// <summary>The JSON-RPC method every update travels under.</summary>
    public const string Method = "session/update";
}
