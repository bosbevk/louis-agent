using System.Net;

namespace LouisAgent.Web.Accounts.Client;

/// <summary>
/// An account route refused: the status, and what it said (the problem's code and messages), so a page can show the
/// right words for a locked account or a weak password.
/// </summary>
public sealed class AccountApiException(HttpStatusCode status, AccountProblem problem)
    : Exception(problem.Detail ?? string.Join(" ", problem.Errors))
{
    public HttpStatusCode Status { get; } = status;

    /// <summary>What the route said went wrong.</summary>
    public AccountProblem Problem { get; } = problem;
}
