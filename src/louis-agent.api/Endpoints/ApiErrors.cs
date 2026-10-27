using LouisAgent.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// Every error the API sends, in the one shape the web apps and the orchestrator read: its kind and the words that say
/// what went wrong.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): the error is the contract's <c>ApiError</c> record, so a handler can
/// declare it in its answers (<c>Results&lt;…, JsonHttpResult&lt;ApiError&gt;&gt;</c>) and a client can read it,
/// where an anonymous object could be neither. The shape is today's, unchanged; Problem Details would change every
/// client's error JSON, so ADR 0045 leaves that for a later version of the API.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class ApiErrors
{
    /// <summary>
    /// The error as a record, for a writer that isn't an endpoint (the key gate, a stream's last line).
    /// </summary>
    public static ApiError Body(string type, string message) => new("error", new ApiErrorDetail(type, message));

    /// <summary>The error as an endpoint's answer, with its status.</summary>
    public static JsonHttpResult<ApiError> Result(int status, string type, string message) =>
        TypedResults.Json(Body(type, message), statusCode: status);
}
