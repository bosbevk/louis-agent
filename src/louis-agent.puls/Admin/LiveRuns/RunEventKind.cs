namespace LouisAgent.Puls.Admin.LiveRuns;

/// <summary>
/// What one of the script's events is about: a question asked, a piece of a turn's stream, a turn that ended, or a
/// conversation's scores. Each travels as its name in lower case.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RunEventKind
{
    Question,
    Sse,
    Turn,
    Conversation
}
