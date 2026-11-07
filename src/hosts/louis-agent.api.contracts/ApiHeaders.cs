namespace LouisAgent.Api.Contracts;

/// <summary>
/// The header the framework's web sends with every call to the API, so a sign-in cookie counts only on a call the
/// app made itself (F61.2): a cross-site form carries the cookie but can't add a header. The engine's key check
/// holds the same two values; a test keeps them equal, since the engine references no contract.
/// </summary>
public static class ApiHeaders
{
    /// <summary>The header's name.</summary>
    public const string RequestedWith = "X-Requested-With";

    /// <summary>The value the framework's web sends.</summary>
    public const string RequestedWithWeb = "louis-agent.web";
}
