namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// The format a session's events are written in (F18 step 1f, CL18; ADR 0024: a choice is an enum, parsed once where it
/// enters). Claude's is the only one; a second waits for a client that needs it.
/// </summary>
public enum StreamFormat
{
    Claude,
}
