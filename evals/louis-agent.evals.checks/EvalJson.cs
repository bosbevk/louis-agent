using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LouisAgent.Evals;

/// <summary>
/// The JSON every eval record uses, snake_case by contract: the scores written into the protocol's lines, the
/// datasets read from files, and a check's settings. One set of options, so they can't drift apart.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class EvalJson
{
    /// <summary>Field names in snake_case, the way every eval file and protocol line writes them.</summary>
    public static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// The same, for what is sent to the API: a null property is left out, so the API's own default holds, and an
    /// enum goes by its name in snake case (<c>record</c>), never by number.
    /// </summary>
    public static readonly JsonSerializerOptions SnakeCaseNoNulls = new(SnakeCase)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    /// <summary>
    /// The same, strict, for reading what a person wrote: an unknown field, a missing required field or a null where
    /// none is allowed is refused, so a typo in a file can't quietly become a default. Its resolver is set, the one the
    /// serializer would pick anyway, so a settings record's JSON schema can be made from it before anything is read.
    /// </summary>
    public static readonly JsonSerializerOptions Strict = new(SnakeCase)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };
}
