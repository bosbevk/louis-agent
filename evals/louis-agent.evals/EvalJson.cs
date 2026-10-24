using System.Text.Json;

namespace LouisAgent.Evals;

/// <summary>
/// The JSON every eval record uses, snake_case by contract: the scores written into the protocol's lines, and the datasets
/// read from files. One set of options, so the two can't drift apart.
/// </summary>
public static class EvalJson
{
    public static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
