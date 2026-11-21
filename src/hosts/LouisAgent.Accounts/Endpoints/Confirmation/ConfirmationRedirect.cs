using LouisAgent.Accounts.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Accounts.Endpoints.Confirmation;

/// <summary>
/// A person who clicks the confirmation mail's link lands on the web's page, not on Identity's plain "Thank you"
/// text: a browser (one that asks for HTML) is sent to <c>/confirm-email?done=true</c>, or <c>?failed=true</c> when
/// the link was bad. A client that asks for JSON or text, such as a test, gets Identity's own answer.
/// </summary>
/// <remarks>
/// An endpoint filter on Identity's route group, which is the one seam Identity's routes leave open: the handler
/// itself is Microsoft's and answers text. The filter reads the answer's type and swaps it for a redirect; nothing
/// of Identity's is copied or changed.
/// </remarks>
public static class ConfirmationRedirect
{
    /// <summary>The route the filter acts on; every other route's answer passes through untouched.</summary>
    private const string ConfirmRoute = "/confirmEmail";

    /// <summary>Puts the redirect on the group's routes.</summary>
    public static TBuilder WithConfirmationRedirect<TBuilder>(this TBuilder routes)
        where TBuilder : IEndpointConventionBuilder
    {
        routes.AddEndpointFilter(Filter);
        return routes;
    }

    /// <summary>Lets every answer through, except a browser's confirmation, which goes to the page.</summary>
    internal static async ValueTask<object?> Filter(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        object? answer = await next(context);
        if (!context.HttpContext.Request.Path.Value!.EndsWith(ConfirmRoute, StringComparison.OrdinalIgnoreCase))
            return answer;
        if (!WantsHtml(context.HttpContext.Request)) return answer;

        // Identity answers Results<ContentHttpResult, UnauthorizedHttpResult>, a wrapper around the one it chose.
        object? chosen = answer is INestedHttpResult nested ? nested.Result : answer;
        return chosen switch
        {
            ContentHttpResult => Results.Redirect(Landing(done: true)),
            UnauthorizedHttpResult => Results.Redirect(Landing(done: false)),
            _ => answer
        };
    }

    /// <summary>A browser following a link asks for HTML first; an API client asks for JSON or nothing in particular.</summary>
    internal static bool WantsHtml(HttpRequest request) =>
        request.Headers.Accept.Any(accept => accept?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>The page, with what happened in its query.</summary>
    internal static string Landing(bool done) => $"/{AccountPages.ConfirmEmail}?{(done ? "done" : "failed")}=true";
}
