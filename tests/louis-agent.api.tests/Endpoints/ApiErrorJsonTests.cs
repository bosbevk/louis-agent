using System.Net;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Endpoints;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the shape every error goes out in, which the web apps and the orchestrator read, and that it is now built as
/// the contract's <c>ApiError</c> record rather than an anonymous object.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class ApiErrorJsonTests
{
    /// <summary>
    /// An unknown route's 404 is written as it always was: the outer type, then the error's type and words.
    /// </summary>
    [Test]
    public async Task AnError_IsWrittenInItsShape_ByteForByte()
    {
        await using var app = ApiTestHost.Build();
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync("/v1/no-such-route");
        string body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        string message = json.RootElement.GetProperty("error").GetProperty("message").GetString()!;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body, Is.EqualTo(
            $$$"""{"type":"error","error":{"type":"not_found_error","message":{{{JsonSerializer.Serialize(message)}}}}}"""));
    }

    /// <summary>
    /// The error is the contract's record, so a handler can declare it and a client can read it, and its kind brings
    /// its status.
    /// </summary>
    [Test]
    public void AnError_IsTheContractsRecord()
    {
        var result = ApiErrors.Conflict("Busy.");

        Assert.That(result, Is.InstanceOf<JsonHttpResult<ApiError>>());
        var typed = (JsonHttpResult<ApiError>)(object)result;
        Assert.That(typed.Value,
            Is.EqualTo(new ApiError("error", new ApiErrorDetail(ApiErrorKind.Conflict.Wire(), "Busy."))));
        Assert.That(typed.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
    }

    /// <summary>
    /// Each kind's method answers its own status with its own kind, so the two can never be mismatched.
    /// </summary>
    [TestCase(nameof(ApiErrors.BadRequest), StatusCodes.Status400BadRequest, ApiErrorKind.InvalidRequest)]
    [TestCase(nameof(ApiErrors.GitFailed), StatusCodes.Status400BadRequest, ApiErrorKind.Git)]
    [TestCase(nameof(ApiErrors.Unauthorized), StatusCodes.Status401Unauthorized, ApiErrorKind.Authentication)]
    [TestCase(nameof(ApiErrors.Forbidden), StatusCodes.Status403Forbidden, ApiErrorKind.Permission)]
    [TestCase(nameof(ApiErrors.NotFound), StatusCodes.Status404NotFound, ApiErrorKind.NotFound)]
    [TestCase(nameof(ApiErrors.Conflict), StatusCodes.Status409Conflict, ApiErrorKind.Conflict)]
    [TestCase(nameof(ApiErrors.TooLarge), StatusCodes.Status413PayloadTooLarge, ApiErrorKind.RequestTooLarge)]
    [TestCase(nameof(ApiErrors.UnsupportedMediaType), StatusCodes.Status415UnsupportedMediaType,
        ApiErrorKind.InvalidRequest)]
    [TestCase(nameof(ApiErrors.Unavailable), StatusCodes.Status503ServiceUnavailable, ApiErrorKind.Api)]
    public void EachKind_BringsItsStatus(string method, int status, object kind)
    {
        // The method by its name, called with a message, as a handler calls it. The kind comes in as an object because
        // the enum is the API's own, and a public test can't name it in its signature.
        var answer = (JsonHttpResult<ApiError>)typeof(ApiErrors).GetMethod(method)!.Invoke(null, ["Why."])!;

        Assert.That((answer.StatusCode, answer.Value!.Error.Type), Is.EqualTo((status, ((ApiErrorKind)kind).Wire())));
    }

    /// <summary>The key gate and a stream's last line write the error themselves, from its kind.</summary>
    [Test]
    public void TheBody_IsTheKindAndTheWords()
    {
        Assert.That(ApiErrors.Body(ApiErrorKind.Authentication, "No key."),
            Is.EqualTo(new ApiError("error", new ApiErrorDetail(ApiErrorKind.Authentication.Wire(), "No key."))));
    }
}
