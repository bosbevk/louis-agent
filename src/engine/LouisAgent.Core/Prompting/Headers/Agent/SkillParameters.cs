using System.Text.Json;

namespace LouisAgent.Core.Prompting.Headers.Agent;

/// <summary>
/// What a skill takes (F97.1): its <c>parameters.schema.json</c> as written, which the model is shown, and what was
/// read from it to check a call, each parameter's type and the ones that must be there.
/// </summary>
/// <param name="Schema">The JSON Schema as the file holds it, kept to show the model unchanged.</param>
/// <param name="Types">Each parameter's type, by its name.</param>
/// <param name="Required">The parameters a call must give.</param>
public sealed record SkillParameters(
    JsonElement Schema,
    IReadOnlyDictionary<string, SkillParameterType> Types,
    IReadOnlyList<string> Required);
