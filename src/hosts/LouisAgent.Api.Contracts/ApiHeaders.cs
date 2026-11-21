namespace LouisAgent.Api.Contracts;

/// <summary>
/// The headers the API and the framework's web agree on. The one the web sends with every call, so a sign-in cookie
/// counts only on a call the app made itself (F61.2): a cross-site form carries the cookie but can't add a header. The
/// engine's key check holds the same two values; a test keeps them equal, since the engine references no contract.
/// And the one the API answers a turn with when the engine paced its words (F156).
/// </summary>
public static class ApiHeaders
{
    /// <summary>The header's name.</summary>
    public const string RequestedWith = "X-Requested-With";

    /// <summary>The value the framework's web sends.</summary>
    public const string RequestedWithWeb = "LouisAgent.Web";

    /// <summary>
    /// The header on a turn's answer that says the engine already paced its words (F156), so a page leaves its own
    /// pacer off and the words are paced once.
    /// </summary>
    public const string StreamPaced = "Stream-Paced";

    /// <summary>Its one value: the words were paced.</summary>
    public const string StreamPacedOn = "on";
}
