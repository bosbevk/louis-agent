using LouisAgent.Choices;

namespace LouisAgent.SessionStream.Wire.Replies.Work;

/// <summary>
/// What a step of a session's work is (F127.4.2): one enum where both sides can see it (ADR 0043), written by its wire
/// name, which the session wire's JSON options write in snake case to match.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum WorkStepKind
{
    /// <summary>The session was asked something: by the agent that opened it, or by a visitor carrying on.</summary>
    [WireName("asked")] Asked,

    /// <summary>It said something.</summary>
    [WireName("said")] Said,

    /// <summary>It called a tool.</summary>
    [WireName("called")] Called,

    /// <summary>A tool gave back its result.</summary>
    [WireName("returned")] Returned
}
