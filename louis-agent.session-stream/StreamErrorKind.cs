using LouisAgent.Choices;

namespace LouisAgent.SessionStream;

/// <summary>
/// What kind of failure ended a turn, as its error event names it in <c>error.type</c>: the API's own error, or a
/// replay with no recorded answer.
/// </summary>
/// <remarks>
/// One enum where both sides can see it (ADR 0043): the API writes a failed turn's kind from it and a stream reader
/// (the eval runner) reads it, so the name is written once (FX26, Louis's pick). Claude's own error event names its
/// kind the same way, <c>api_error</c>, <c>overloaded_error</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum StreamErrorKind
{
    /// <summary>The API's own failure: the turn threw something that isn't a replay miss.</summary>
    [WireName("api_error")] Api,

    /// <summary>A replay asked the model something it has no recorded answer for; the model wasn't called.</summary>
    [WireName("replay_miss_error")] ReplayMiss,
}
