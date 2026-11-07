using System.Text.Json;

namespace LouisAgent.Web.Accounts;

/// <summary>
/// What an account route said went wrong, read from its problem-details body (RFC 7807, the shape Identity's routes
/// answer): a code in <c>detail</c> for a refused sign-in (<c>Failed</c>, <c>LockedOut</c>, <c>NotAllowed</c>,
/// <c>RequiresTwoFactor</c>), or the messages under <c>errors</c> for a body that didn't pass.
/// </summary>
public sealed record AccountProblem(string? Detail, IReadOnlyList<string> Errors)
{
    /// <summary>Nothing readable: an empty body, or not JSON.</summary>
    public static readonly AccountProblem None = new(null, []);

    /// <summary>Reads the body; anything that isn't a problem document reads as <see cref="None"/>.</summary>
    public static AccountProblem Read(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return None;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return None;
            return new AccountProblem(DetailOf(json.RootElement), ErrorsOf(json.RootElement));
        }
        catch (JsonException)
        {
            return None;
        }
    }

    /// <summary>The <c>detail</c>, when it's text.</summary>
    private static string? DetailOf(JsonElement root) =>
        root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String
            ? detail.GetString()
            : null;

    /// <summary>Every message under <c>errors</c>, in order; Identity keys them by its own error codes.</summary>
    private static IReadOnlyList<string> ErrorsOf(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object) return [];
        return errors.EnumerateObject()
            .SelectMany(field => field.Value.ValueKind == JsonValueKind.Array
                ? field.Value.EnumerateArray().Select(e => e.GetString()).OfType<string>()
                : [])
            .ToList();
    }
}
