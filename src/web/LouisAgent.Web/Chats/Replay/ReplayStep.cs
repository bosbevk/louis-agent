using LouisAgent.SessionStream;

namespace LouisAgent.Web.Chats.Replay;

/// <summary>One event of a replay and how long to wait before it is drawn.</summary>
/// <param name="Event">The event, as the stream's reader hands it over.</param>
/// <param name="WaitMs">The wait before it, in milliseconds.</param>
public sealed record ReplayStep(SessionEvent Event, int WaitMs);
