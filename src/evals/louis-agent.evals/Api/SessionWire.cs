namespace LouisAgent.Evals.Api;

/// <summary>
/// The replies the eval client reads from the API's session endpoints (F18 step 5.1), as records, read with
/// <see cref="EvalJson.SnakeCase"/>. What it sends is the session wire's shared records since F127.3.1.
/// </summary>
internal static class SessionWire
{
    /// <summary>A session opened: its id.</summary>
    internal sealed record Opened(string SessionId);

    /// <summary>The toolsets the API knows.</summary>
    internal sealed record ToolsetsReply(IReadOnlyList<ToolsetInfo> Toolsets);

    /// <summary>One toolset, and whether this instance has its settings.</summary>
    internal sealed record ToolsetInfo(string Name, bool Available);

    /// <summary>An error the API answered with.</summary>
    internal sealed record ErrorReply(ErrorDetail? Error);

    /// <summary>What the error says.</summary>
    internal sealed record ErrorDetail(string? Message);
}
