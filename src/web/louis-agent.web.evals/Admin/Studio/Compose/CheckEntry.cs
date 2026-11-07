namespace LouisAgent.Web.Evals.Admin.Studio.Compose;

/// <summary>
/// One check of an eval being composed: its type from the catalogue, the name the file gives it when it renames one,
/// and each setting's value as the person typed it, by the setting's name.
/// </summary>
/// <remarks>A class: the form's inputs bind to it and change it as the person types (ADR 0040).</remarks>
public sealed class CheckEntry
{
    /// <summary>The check's type, from the catalogue.</summary>
    public string Type { get; set; } = "";

    /// <summary>The name the file gives the check, or none to keep its type's.</summary>
    public string Name { get; set; } = "";

    /// <summary>Each setting's value as typed; an empty one is left out of the file.</summary>
    public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
}
