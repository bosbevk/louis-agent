namespace LouisAgent.Evals.Trials;

/// <summary>
/// What a turn's record in the protocol says about it: who answered, the sub-session it continued, the calls the
/// mode's agent made (the protocol's <c>agent_calls</c>) and the specialists' calls, the hosts they read, and the
/// error. The runner writes them.
/// </summary>
/// <remarks>
/// <see cref="Of"/> reads them from the engine's events for every eval (F49.2, finding B), so an eval in a file needs
/// no code for them; an eval in C# may still say its own.
/// </remarks>
public sealed record TurnFacts(
    string? Role,
    string? Continued,
    int AgentCalls,
    int SpecialistCalls,
    IReadOnlyList<string> Hosts,
    string? Error)
{
    /// <summary>
    /// The facts the engine's events give: the role of its first hand-over, the sub-session it went back to, both
    /// sides' calls, the hosts of the URLs its tools were given (in order, once each, without "www."), and its error.
    /// </summary>
    public static TurnFacts Of(TurnTranscript turn) => new(
        EngineEvents.RoutedRole(turn),
        EngineEvents.ContinuedHandle(turn),
        EngineEvents.ToolsCalled(turn).Count,
        EngineEvents.SpecialistToolsCalled(turn).Count,
        [.. EngineEvents.UrlsGiven(turn).Select(HostOf).Where(host => host.Length > 0).Distinct()],
        EngineEvents.ErrorTextOf(turn) is { Length: > 0 } error ? error : null);

    /// <summary>A URL's host in lower case without "www.", or empty when it isn't an absolute URL.</summary>
    private static string HostOf(string url)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : "";
        return host.StartsWith("www.", StringComparison.Ordinal) ? host["www.".Length..] : host;
    }
}
