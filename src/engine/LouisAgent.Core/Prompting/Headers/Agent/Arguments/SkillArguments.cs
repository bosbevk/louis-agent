using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Prompting.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Arguments;

namespace LouisAgent.Core.Prompting.Headers.Agent.Arguments;

/// <summary>
/// Checks a call's arguments against a skill's parameters before its script runs (F97.1): every required one there,
/// none the skill doesn't take, and each value of its parameter's type. Every problem is named, not only the first, so
/// the model fixes the call in one go; a call with none runs.
/// </summary>
/// <remarks>
/// Our own check, no JSON Schema package (Louis's pick, 2026-11-26): a skill's parameters are flat, each with one of
/// JSON's six types, and that is all this reads; what a schema says beyond it is the script's to check.
/// </remarks>
/// <seealso href="https://json-schema.org/understanding-json-schema/reference/type">JSON Schema: type-specific keywords</seealso>
public static class SkillArguments
{
    /// <summary>JSON's word for a value that is none of the six types, as a call gives it.</summary>
    private const string NullKind = "null";

    /// <summary>What is wrong with these arguments; none when the call is good.</summary>
    public static IReadOnlyList<string> Problems(SkillParameters parameters, JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
            return [MessageText.Format(SkillToolMessage.ArgumentsNotAnObject)];

        var given = arguments.EnumerateObject().ToList();
        return
        [
            .. parameters.Required.Where(name => given.All(g => g.Name != name)).Select(Missing),
            .. given.Where(g => !parameters.Types.ContainsKey(g.Name)).Select(g => Unknown(g.Name, parameters)),
            .. given.Where(g => parameters.Types.TryGetValue(g.Name, out var type) && !Fits(type, g.Value))
                .Select(g => WrongType(g, parameters.Types[g.Name]))
        ];
    }

    /// <summary>Whether a value is of this type; a whole number fits an integer, any number a number.</summary>
    private static bool Fits(SkillParameterType type, JsonElement value) => type switch
    {
        SkillParameterType.String => value.ValueKind == JsonValueKind.String,
        SkillParameterType.Number => value.ValueKind == JsonValueKind.Number,
        SkillParameterType.Integer => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        SkillParameterType.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        SkillParameterType.Array => value.ValueKind == JsonValueKind.Array,
        SkillParameterType.Object => value.ValueKind == JsonValueKind.Object,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>The kind of value a call gave, in JSON Schema's words, for the model to see what it sent.</summary>
    private static string KindOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => SkillParameterType.String.Wire(),
        JsonValueKind.Number when value.TryGetInt64(out _) => SkillParameterType.Integer.Wire(),
        JsonValueKind.Number => SkillParameterType.Number.Wire(),
        JsonValueKind.True or JsonValueKind.False => SkillParameterType.Boolean.Wire(),
        JsonValueKind.Array => SkillParameterType.Array.Wire(),
        JsonValueKind.Object => SkillParameterType.Object.Wire(),
        _ => NullKind
    };

    /// <summary>A required parameter the call left out.</summary>
    private static string Missing(string name)
    {
        var leftOut = new RequiredArgumentLeftOutHoles(name);
        return MessageText.Format(SkillToolMessage.RequiredArgumentLeftOut, leftOut);
    }

    /// <summary>An argument the skill doesn't take, with the ones it does.</summary>
    private static string Unknown(string name, SkillParameters parameters)
    {
        var unknown = new ArgumentUnknownHoles(name, string.Join(", ", parameters.Types.Keys));
        return MessageText.Format(SkillToolMessage.ArgumentUnknown, unknown);
    }

    /// <summary>An argument whose value isn't its parameter's type.</summary>
    private static string WrongType(JsonProperty argument, SkillParameterType type)
    {
        var wrongType = new ArgumentWrongTypeHoles(argument.Name, type.Wire(), KindOf(argument.Value));
        return MessageText.Format(SkillToolMessage.ArgumentWrongType, wrongType);
    }
}
