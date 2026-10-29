using LouisAgent.Choices;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// What kind of error the API is sending, in the words the web apps and the orchestrator already read. Each kind is
/// named once, here, so a typo can't reach a client.
/// </summary>
/// <remarks>
/// Replace Primitive with Object (Fowler): the error's type was a string typed at every refusal, so a misspelt one
/// compiled and went out. As an enum it is checked by the compiler, and its wire text is today's text, so no client
/// sees a change. One member per text on the wire: a 400 and a 415 both say <c>invalid_request_error</c>, so they
/// share <see cref="InvalidRequest"/>, and <c>ApiErrors</c>' methods say which status goes with it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
internal enum ApiErrorKind
{
    /// <summary>The request asked for something we can't do as asked (a 400, or a 415 for a binary file).</summary>
    [WireName("invalid_request_error")] InvalidRequest,

    /// <summary>git refused, and its words go back as the message.</summary>
    [WireName("git_error")] Git,

    /// <summary>No key, or the wrong one.</summary>
    [WireName("authentication_error")] Authentication,

    /// <summary>The caller may not have this, whatever key it sends (a secret file, a closed portal).</summary>
    [WireName("permission_error")] Permission,

    /// <summary>There's nothing by that name.</summary>
    [WireName("not_found_error")] NotFound,

    /// <summary>It can't happen now, because something else is going on (a run, a turn, a branch in use).</summary>
    [WireName("conflict_error")] Conflict,

    /// <summary>It's too big to send.</summary>
    [WireName("request_too_large")] RequestTooLarge,

    /// <summary>Something on our side isn't there or didn't answer (the store, a turn that broke).</summary>
    [WireName("api_error")] Api,
}
