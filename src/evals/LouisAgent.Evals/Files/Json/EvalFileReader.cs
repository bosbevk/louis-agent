using System.Text.Json;

namespace LouisAgent.Evals.Files.Json;

/// <summary>Reads an eval file's text into its records, strictly, with <see cref="EvalJson.Options"/>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class EvalFileReader
{
    /// <summary>The eval the text holds; a file that is only <c>null</c> is refused too.</summary>
    public static EvalFile Read(string json) =>
        JsonSerializer.Deserialize<EvalFile>(json, EvalJson.Options) ?? throw new JsonException();
}
