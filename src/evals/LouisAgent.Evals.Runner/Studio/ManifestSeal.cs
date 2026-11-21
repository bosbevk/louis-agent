using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Evals.Datasets.Manifest;

namespace LouisAgent.Evals.Runner.Studio;

/// <summary>
/// Seals a dataset manifest the studio wrote without its hash: the content hash of the scenarios is written into it,
/// as a person does by hand when a dataset changes. A manifest that has a hash keeps it, so a scenario changed after
/// sealing is still refused as a change to bump.
/// </summary>
/// <remarks>
/// The hash is the harness's (<see cref="DatasetHash"/>), and the API never carries the harness (finding R), so the
/// studio leaves it empty and the runner, its one reader of eval files, fills it in. The manifest is read as a
/// <see cref="JsonObject"/> to change one field and keep the rest as written (ADR 0041: a node for an edit in place).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ManifestSeal
{
    /// <summary>The field that holds the scenarios' hash.</summary>
    private const string HashField = "content_hash";

    /// <summary>How a sealed manifest is written back: indented, as the eval folders keep theirs.</summary>
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>
    /// Writes the scenarios' hash into the folder's manifest when it has none; anything else is left.
    /// </summary>
    /// <remarks>
    /// Guard clauses: no manifest, not an object, or a hash already there each leave the file as it is.
    /// </remarks>
    public static void Seal(string evalFolder)
    {
        string dataset = Path.Combine(evalFolder, "Dataset");
        string manifest = Path.Combine(dataset, "dataset.json");
        if (!File.Exists(manifest)) return;
        if (JsonNode.Parse(File.ReadAllText(manifest)) is not JsonObject node) return;
        if (node[HashField] is JsonValue value && value.TryGetValue(out string? hash) && hash.Length > 0) return;

        node[HashField] = DatasetHash.Of(Path.Combine(dataset, "scenarios"));
        File.WriteAllText(manifest, node.ToJsonString(Indented) + "\n");
    }
}
