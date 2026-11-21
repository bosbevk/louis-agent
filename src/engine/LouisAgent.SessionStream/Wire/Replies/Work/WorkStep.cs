namespace LouisAgent.SessionStream.Wire.Replies.Work;

/// <summary>
/// One step of a session's work: what kind, its text (what was asked or said, what was sent a tool, what came back),
/// the tool's name for a call or a result, and whether a result was an error.
/// </summary>
public sealed record WorkStep(WorkStepKind Kind, string Text, string? Tool = null, bool Failed = false);
