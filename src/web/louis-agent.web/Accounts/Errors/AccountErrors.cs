using LouisAgent.Web.Text;

namespace LouisAgent.Web.Accounts.Errors;

/// <summary>
/// What a page says when an account call failed: Identity's codes become the site's words, a body that didn't pass
/// shows its messages, and anything else is "something went wrong".
/// </summary>
/// <remarks>
/// A switch on the shape (ADR 0051), one arm per case, kept out of the components so it's tested on its own and every
/// page says the same thing for the same failure.
/// </remarks>
public static class AccountErrors
{
    /// <summary>The words for this failure.</summary>
    public static AccountErrorView Of(Exception exception, AccountText text) => exception switch
    {
        AccountApiException { Problem.Detail: "Failed" } => new AccountErrorView(text.Failed, []),
        AccountApiException { Problem.Detail: "LockedOut" } => new AccountErrorView(text.LockedOut, []),
        AccountApiException { Problem.Detail: "NotAllowed" } => new AccountErrorView(text.NotAllowed, []),
        AccountApiException { Problem.Detail: "RequiresTwoFactor" } => new AccountErrorView(text.RequiresTwoFactor, []),
        AccountApiException { Problem.Errors.Count: > 0 } api => new AccountErrorView(null, api.Problem.Errors),
        _ => new AccountErrorView(text.SomethingWentWrong, [])
    };
}

/// <summary>A line to say, or the messages to list, for the notice on the page.</summary>
public sealed record AccountErrorView(string? Message, IReadOnlyList<string> Details);
