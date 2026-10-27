using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals;

/// <summary>
/// The JSON every eval record uses, snake_case by contract: the scores written into the protocol's lines, and the datasets
/// read from files. One set of options, so the two can't drift apart.
/// </summary>
public static class EvalJson
{
    public static readonly JsonSerializerOptions SnakeCase = new()
    { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>
    /// The same, for what is sent to the API (ADR 0041): a null property is left out, so the API's own default holds, and an enum goes by
    /// its name in snake case (<c>record</c>), never by number.
    /// </summary>
    public static readonly JsonSerializerOptions SnakeCaseNoNulls = new(SnakeCase)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
