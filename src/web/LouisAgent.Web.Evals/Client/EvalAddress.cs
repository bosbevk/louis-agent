namespace LouisAgent.Web.Evals.Client;

/// <summary>
/// Every address of the eval pages and every eval route the pages read, in one place, each name escaped, so no page
/// joins an address by hand. A repeat is part of a conversation's address, never a <c>#</c>, so it can be linked
/// and the browser's Back works.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public static class EvalAddress
{
    /// <summary>The evals' page, one card an eval.</summary>
    public const string EvalsPage = "/admin/evals";

    /// <summary>The route that lists the host's evals.</summary>
    public const string EvalsRoute = "v1/admin/evals";

    /// <summary>A run's page.</summary>
    public static string RunPageOf(string eval, string run) => $"{EvalsPage}/{Escaped(eval)}/runs/{Escaped(run)}";

    /// <summary>A conversation's page in a run, at a repeat; with none, the page picks the repeat to open.</summary>
    public static string ConversationPageOf(string eval, string run, string conversation, int? repeat = null) =>
        repeat is null
            ? $"{RunPageOf(eval, run)}/{Escaped(conversation)}"
            : $"{RunPageOf(eval, run)}/{Escaped(conversation)}/{repeat}";

    /// <summary>The route that reads one run.</summary>
    public static string RunRouteOf(string run) => $"{EvalsRoute}/runs/{Escaped(run)}";

    /// <summary>The route that lists an eval's runs, the newest first.</summary>
    public static string RunsRouteOf(string eval) => $"{EvalsRoute}/{Escaped(eval)}/runs";

    /// <summary>One part of an address, escaped so a name can't change the address's shape.</summary>
    private static string Escaped(string part) => Uri.EscapeDataString(part);
}
