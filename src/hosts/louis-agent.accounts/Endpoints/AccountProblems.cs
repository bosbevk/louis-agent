using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.Endpoints;

/// <summary>
/// A refusal in the shape Identity's own routes use (a problem document with the messages under <c>errors</c>), so
/// the browser reads ours and Identity's alike: one of our texts under a field's name, or Identity's result as it is.
/// </summary>
public static class AccountProblems
{
    /// <summary>One of our messages, under the field it's about, with its holes filled.</summary>
    public static ValidationProblem Of(string field, Enum message, object? values = null) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [MessageText.Format(message, values)]
        });

    /// <summary>Identity's errors as it wrote them, each under its own code.</summary>
    public static ValidationProblem Of(IdentityResult result) =>
        TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
}
