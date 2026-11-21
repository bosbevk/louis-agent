using System.Text.Json;
using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.Diagrams.Sources;

/// <summary>
/// Writes a diagram as the JSON file its doc keeps beside it, through the reader's own options (CL60.2), so what the
/// docs' import writes is what a person would: indented, choices by name, nothing left at its default.
/// </summary>
public static class DiagramWriter
{
    /// <summary>The diagram's file, ending in a new line.</summary>
    public static string Write(DiagramSource source)
    {
        // A kind's own type is its default, which the reader fills in again, so it is left out like any other.
        var written = source.Type == DiagramTypes.Own(source.Kind) ? source with { Type = null } : source;

        return JsonSerializer.Serialize(written, DiagramJson.Options).ReplaceLineEndings("\n") + "\n";
    }
}
