namespace LouisAgent.Core.Guardrails.Auth;

using System.Security.Cryptography;
using System.Text;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;

/// <summary>
/// The rule for a host that serves the agent's tools over the network (the API; the MCP server over HTTP), FX18: it
/// fails closed. Without a key it refuses to start, unless <c>AGENT_AUTH=off</c> says it may be open on purpose; a key
/// that is set is always checked, whatever the mode. Pure, so it is tested without a web host, as <c>AdminGate</c> is:
/// core references no ASP.NET Core, so a host hands it the two header values.
/// </summary>
public static class HostAuth
{
    /// <summary>The names a "supported" message lists, from the enum itself (ADR 0024).</summary>
    public static IReadOnlyList<string> SupportedModes { get; } = SettingChoice.Names<AuthMode>();

    /// <summary>
    /// The key a host checks: the first candidate that isn't blank, trimmed, else null. "Set but empty" (a bare
    /// <c>MCP_API_KEY=</c> in an env file) is not a key, so a fallback (<c>AGENT_API_KEY</c>) still counts; <c>??</c> alone
    /// falls back only from null, and left one fail-open path (the step's review).
    /// </summary>
    public static string? Key(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))?.Trim();

    /// <summary>AGENT_AUTH, parsed once where it enters: unset is <see cref="AuthMode.Required"/>; anything unknown stops start-up.</summary>
    public static AuthMode Mode(string? setting)
    {
        if (!SettingChoice.TryParse(setting, out AuthMode? mode))
            throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownAuthMode,
                new { Value = setting?.Trim(), Supported = string.Join(" or ", SupportedModes) }));
        return mode ?? AuthMode.Required;
    }

    /// <summary>What start-up does: a key set is always <see cref="HostAuthDecision.Start"/>; none is a refusal unless the mode is off.</summary>
    public static HostAuthDecision AtStartup(AuthMode mode, string? key) =>
        !string.IsNullOrWhiteSpace(key) ? HostAuthDecision.Start
        : mode == AuthMode.Off ? HostAuthDecision.StartOpen
        : HostAuthDecision.Refuse;

    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Whether a request presents the key: <c>x-api-key</c>, else <c>Authorization</c> with a leading <c>Bearer </c> taken
    /// off (a bare key there still passes, as it always did). Compared in fixed time, so a wrong key reveals nothing by
    /// timing; a different length is simply wrong.
    /// </summary>
    public static bool HasKey(string? apiKeyHeader, string? authorization, string expected)
    {
        string presented = apiKeyHeader
                           ?? (authorization is { } auth && auth.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
                               ? auth[BearerPrefix.Length..] : authorization)
                           ?? string.Empty;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(expected));
    }
}
