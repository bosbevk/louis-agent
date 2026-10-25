using LouisAgent.Core.Sessions.Replay;

namespace LouisAgent.Evals.Api;

/// <summary>
/// The JSON the API's session endpoints send and answer (F18 step 5.1), as records: serialized with <see cref="EvalJson.SnakeCaseNoNulls"/>,
/// so a null field is left out and the API's defaults hold (as the Python's <c>if v is not None</c>), and read with <see cref="EvalJson.SnakeCase"/>.
/// </summary>
internal static class SessionWire
{
    internal sealed record OpenBody(IReadOnlyList<string> Toolsets, string Role, string TimeZone, string Task, string Run, string? Location,
        string? Llm, ReplayBody Replay);

    /// <summary><c>Web</c> is null (left out) when the instance's WEB_REPLAY decides; <c>Model</c> is always sent.</summary>
    internal sealed record ReplayBody(WebReplayMode? Web, string? Of, bool Model);

    internal sealed record MessageBody(string Message);

    internal sealed record Opened(string SessionId);

    internal sealed record ToolsetsReply(IReadOnlyList<ToolsetInfo> Toolsets);

    internal sealed record ToolsetInfo(string Name, bool Available);

    internal sealed record ErrorReply(ErrorDetail? Error);

    internal sealed record ErrorDetail(string? Message);
}
