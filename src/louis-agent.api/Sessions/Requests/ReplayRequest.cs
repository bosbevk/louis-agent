namespace LouisAgent.Api.Sessions.Requests;

/// <summary>
/// What an eval session asks of the web (F18 step 4a), the <c>replay</c> of <c>POST /sessions</c>: <c>web</c> (off, record or replay;
/// none sent: the instance's <c>WEB_REPLAY</c>), <c>of</c> (the recorded trial: a replay reads it, a sub-session records into its
/// parent's) and <c>model</c> (replay the model's answers too; refused until step 4e's cache). Only a session with a run tag may send one.
/// </summary>
internal sealed record ReplayRequest(string? Web, string? Of, bool Model = false);
