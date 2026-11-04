using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;

namespace LouisAgent.Api.Endpoints.Answers;

/// <summary>
/// A 201: says where the new thing lives and writes it with the JSON options of the wire it's on.
/// </summary>
/// <remarks>
/// A custom <c>IResult</c>, Microsoft's way to shape an answer the built-in ones don't. <c>TypedResults.Created</c>
/// writes with the app's default options, which on the admin wire would turn <c>exit_code</c> into <c>exitCode</c>;
/// this one takes the wire's options, so one options object per wire holds for a 201 too (the unattended run's
/// finding 2). It tells OpenAPI what it produces, as the framework's own answers do.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class CreatedJson<TValue>(string location, TValue value, JsonSerializerOptions options)
    : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>, IEndpointMetadataProvider
{
    /// <summary>Where the new thing lives.</summary>
    public string Location => location;

    /// <summary>Always 201.</summary>
    public int StatusCode => StatusCodes.Status201Created;

    /// <summary>The new thing, as it is written.</summary>
    public TValue Value => value;

    /// <summary>The status, for a reader that only knows status answers.</summary>
    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <summary>The value, for a reader that only knows value answers.</summary>
    object? IValueHttpResult.Value => value;

    /// <summary>The value, for a reader that knows its type.</summary>
    TValue? IValueHttpResult<TValue>.Value => value;

    /// <summary>Writes the 201: its status, its Location, and the value as JSON with the wire's options.</summary>
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.Headers.Location = location;
        return context.Response.WriteAsJsonAsync(value, options);
    }

    /// <summary>Tells OpenAPI the route answers 201 with this type as JSON.</summary>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status201Created, typeof(TValue),
            ["application/json"]));
}
