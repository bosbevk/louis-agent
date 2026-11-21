using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Choices;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// What <c>--checks</c> prints, the catalogue the studio composes an eval from: every check type, in the catalogue's
/// order, with its kind and the JSON schema of its settings, then every reader with the kind it reads. One JSON object
/// a line, as <c>--list</c> writes its evals.
/// </summary>
/// <remarks>
/// Facade (Gang of Four) over the two catalogues, for the hosts that reach the harness only through the runner's
/// process (finding R): the API reads these lines and never the catalogues themselves. A line is a check when it has
/// a <c>type</c>, a reader when it has a <c>reader</c>, so one reader of the lines tells them apart.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class ChecksListing
{
    /// <summary>The check lines, then the reader lines.</summary>
    public static IEnumerable<string> Lines(CheckCatalog checks, ReaderCatalog readers) =>
    [
        .. checks.Listing().Select(check =>
            JsonSerializer.Serialize(new CheckLine(check.Type, check.Kind.Wire(), check.Parameters),
                EvalJson.SnakeCase)),
        .. readers.Listing().Select(reader =>
            JsonSerializer.Serialize(new ReaderLine(reader.Name, reader.Kind.Wire()), EvalJson.SnakeCase))
    ];

    /// <summary>A check type's line: its type, its kind's wire name and its settings' schema.</summary>
    private sealed record CheckLine(string Type, string Kind, JsonNode Parameters);

    /// <summary>A reader's line: its name and its kind's wire name.</summary>
    private sealed record ReaderLine(string Reader, string Kind);
}
