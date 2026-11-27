using System.Globalization;
using System.Net;
using System.Text.Json;
using Anthropic.Exceptions;
using Anthropic.Models;
using LouisAgent.Core.Llm.Failures;

namespace LouisAgent.Core.Anthropic.Failures;

/// <summary>
/// Claude's failures, named (F8). An overload or a server error that arrives inside a stream is worth asking again:
/// the SDK retries what fails before a stream, but not what fails inside one. An account limit is any of the three
/// ways Anthropic says so: a spend limit a person set (a 400 whose message starts "You have reached your specified"),
/// the tier's monthly cap (a 429 with <c>enforced_spend_limit_reached</c>), and a billing problem (a 402). Anything
/// else is neither, the SDK's own retries having already had their go.
/// </summary>
/// <remarks>
/// The <see cref="IModelFailures"/> strategy made real for Anthropic (ADR 0003), reading the SDK's own exceptions, so
/// Anthropic's error shapes and wording stay in this library. The words are Anthropic's, from its
/// <a href="https://platform.claude.com/docs/en/api/rate-limits">rate limits</a> page.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class AnthropicFailures : IModelFailures
{
    /// <summary>How a spend limit a person set starts its message, a workspace's or the whole organisation's.</summary>
    private const string SpendLimitSet = "You have reached your specified";

    /// <summary>The error code of the tier's monthly spend cap.</summary>
    private const string SpendCapReached = "enforced_spend_limit_reached";

    /// <summary>
    /// The words before the time an account limit lifts: "You will regain access on 2026-12-01 at 00:00 UTC".
    /// </summary>
    private const string RegainAccessOn = "regain access on ";

    /// <inheritdoc />
    public ModelFailure Of(Exception failure) => failure switch
    {
        AnthropicSseException { ErrorType: ErrorType.OverloadedError or ErrorType.ApiError } => ModelFailure.Transient,
        AnthropicApiException { StatusCode: HttpStatusCode.PaymentRequired } api => LimitOf(api),
        AnthropicApiException { StatusCode: HttpStatusCode.BadRequest } api when MessageOf(api) is { } message &&
            message.StartsWith(SpendLimitSet, StringComparison.Ordinal) => LimitOf(api),
        AnthropicApiException { StatusCode: HttpStatusCode.TooManyRequests } api when CodeOf(api) == SpendCapReached =>
            LimitOf(api),
        _ => ModelFailure.Other
    };

    /// <summary>An account limit, in Anthropic's words, with the time it lifts when the words say.</summary>
    private static ModelFailure LimitOf(AnthropicApiException api)
    {
        string reason = MessageOf(api) ?? api.Message;
        return ModelFailure.AccountLimit(reason, ResumeOf(reason));
    }

    /// <summary>The error's own message from the answer's body; none when the body isn't Anthropic's error.</summary>
    private static string? MessageOf(AnthropicApiException api) =>
        ErrorOf(api) is { } error && error.TryGetProperty("message", out var message) &&
        message.ValueKind == JsonValueKind.String
            ? message.GetString()
            : null;

    /// <summary>The error's code from the answer's body (<c>error.details.error_code</c>), when it has one.</summary>
    private static string? CodeOf(AnthropicApiException api) =>
        ErrorOf(api) is { } error && error.TryGetProperty("details", out var details) &&
        details.ValueKind == JsonValueKind.Object && details.TryGetProperty("error_code", out var code) &&
        code.ValueKind == JsonValueKind.String
            ? code.GetString()
            : null;

    /// <summary>
    /// The <c>error</c> object of the answer's body, read as input we don't own (a <see cref="JsonElement"/>); none
    /// when the body isn't JSON or has no such object.
    /// </summary>
    private static JsonElement? ErrorOf(AnthropicApiException api)
    {
        try
        {
            using var body = JsonDocument.Parse(api.ResponseBody);
            return body.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
                ? error.Clone()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// When access comes back, from Anthropic's sentence "You will regain access on 2026-12-01 at 00:00 UTC"; none
    /// when the words aren't there.
    /// </summary>
    private static DateTimeOffset? ResumeOf(string reason)
    {
        int at = reason.IndexOf(RegainAccessOn, StringComparison.Ordinal);
        if (at < 0) return null;
        // "2026-12-01 at 00:00 UTC": the date, the word, the time, all in UTC.
        string rest = reason[(at + RegainAccessOn.Length)..];
        string[] parts = rest.Split(' ', 4);
        if (parts.Length < 3) return null;
        return DateTimeOffset.TryParseExact($"{parts[0]} {parts[2]}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var resumes)
            ? resumes
            : null;
    }
}
