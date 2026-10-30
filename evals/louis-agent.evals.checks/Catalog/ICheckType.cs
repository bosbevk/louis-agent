using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// One kind of check you can name from a file: its type ("within-time"), how it decides, the record its settings
/// are read into, and a way to build the check from those settings.
/// </summary>
/// <remarks>
/// Factory Method: the catalogue only knows this interface, and each entry decides how its own check is built. That
/// is what lets a pack add a check type later without the harness learning about it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public interface ICheckType
{
    /// <summary>The name a file uses for it, in kebab case ("within-time").</summary>
    string Type { get; }

    /// <summary>How its checks decide: a fixed rule, a model or a person.</summary>
    CheckKind Kind { get; }

    /// <summary>The record its settings are read into, so a form can be made from it.</summary>
    Type Parameters { get; }

    /// <summary>Builds the check from its settings as they appear in the eval file.</summary>
    ICheck Create(JsonElement parameters);
}
