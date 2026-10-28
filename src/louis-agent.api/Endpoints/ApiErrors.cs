using LouisAgent.Api.Contracts;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// Every error the API sends, in the one shape the web apps and the orchestrator read: its kind and the words that say
/// what went wrong. Pick the method for what happened (<c>NotFound</c>, <c>Conflict</c>…) and it brings its status.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): the error is the contract's <c>ApiError</c> record, so a handler can
/// declare it in its answers (<c>Results&lt;…, JsonHttpResult&lt;ApiError&gt;&gt;</c>) and a client can read it,
/// where an anonymous object could be neither. The shape is today's, unchanged; Problem Details would change every
/// client's error JSON, so ADR 0045 leaves that for a later version of the API.
/// Remove Flag Argument (Fowler), the way <c>TypedResults.NotFound</c> works: one method per kind of error, each with
/// its status, instead of one method that takes a status and a kind. A caller can't pair a 404 with a conflict, and
/// the call says what happened.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal static class ApiErrors
{
    /// <summary>
    /// The error as a record, for a writer that isn't an endpoint (the key gate, a stream's last line).
    /// </summary>
    public static ApiError Body(ApiErrorKind kind, string message) =>
        new("error", new ApiErrorDetail(kind.Wire(), message));

    /// <summary>A 400: the request can't be done as asked.</summary>
    public static JsonHttpResult<ApiError> BadRequest(string message) =>
        Answer(StatusCodes.Status400BadRequest, ApiErrorKind.InvalidRequest, message);

    /// <summary>A 400 with git's own words: git refused.</summary>
    public static JsonHttpResult<ApiError> GitFailed(string message) =>
        Answer(StatusCodes.Status400BadRequest, ApiErrorKind.Git, message);

    /// <summary>A 401: no key, or the wrong one.</summary>
    public static JsonHttpResult<ApiError> Unauthorized(string message) =>
        Answer(StatusCodes.Status401Unauthorized, ApiErrorKind.Authentication, message);

    /// <summary>A 403: not for this caller, whatever key it sends.</summary>
    public static JsonHttpResult<ApiError> Forbidden(string message) =>
        Answer(StatusCodes.Status403Forbidden, ApiErrorKind.Permission, message);

    /// <summary>A 404: there's nothing by that name.</summary>
    public static JsonHttpResult<ApiError> NotFound(string message) =>
        Answer(StatusCodes.Status404NotFound, ApiErrorKind.NotFound, message);

    /// <summary>A 409: not now, something else is going on.</summary>
    public static JsonHttpResult<ApiError> Conflict(string message) =>
        Answer(StatusCodes.Status409Conflict, ApiErrorKind.Conflict, message);

    /// <summary>A 413: too big to send.</summary>
    public static JsonHttpResult<ApiError> TooLarge(string message) =>
        Answer(StatusCodes.Status413PayloadTooLarge, ApiErrorKind.RequestTooLarge, message);

    /// <summary>A 415: a file we can't show as text.</summary>
    public static JsonHttpResult<ApiError> UnsupportedMediaType(string message) =>
        Answer(StatusCodes.Status415UnsupportedMediaType, ApiErrorKind.InvalidRequest, message);

    /// <summary>A 503: something on our side isn't there or didn't answer.</summary>
    public static JsonHttpResult<ApiError> Unavailable(string message) =>
        Answer(StatusCodes.Status503ServiceUnavailable, ApiErrorKind.Api, message);

    /// <summary>The error as an endpoint's answer, with its status.</summary>
    private static JsonHttpResult<ApiError> Answer(int status, ApiErrorKind kind, string message) =>
        TypedResults.Json(Body(kind, message), statusCode: status);
}
