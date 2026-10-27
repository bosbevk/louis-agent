using System.Net;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Endpoints;
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

    /// <summary>The error is the contract's record, so a handler can declare it and a client can read it.</summary>
    [Test]
    public void AnError_IsTheContractsRecord()
    {
        var result = ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", "Busy.");

        Assert.That(result, Is.InstanceOf<JsonHttpResult<ApiError>>());
        var typed = (JsonHttpResult<ApiError>)(object)result;
        Assert.That(typed.Value, Is.EqualTo(new ApiError("error", new ApiErrorDetail("conflict_error", "Busy."))));
        Assert.That(typed.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
    }
}
