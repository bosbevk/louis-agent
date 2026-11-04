using LouisAgent.Composition.Messages;
using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Evals.Drafts;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// Writes an eval being composed out as the folder the runner reads (ADR 0058): its file as typed, its rubrics under
/// <c>Rubrics/</c>, a manifest for its dataset with the hash left for the runner to seal, the eval's sample drafts as
/// its dev scenarios and its transcript drafts under the names they were kept with. Each draft has a folder of its own
/// under the root, written afresh every time, so a file the draft dropped is gone. An edit of an eval the workspace
/// already has starts from a copy of its folder (FX32), so what git holds and the studio doesn't keep (its reference
/// transcripts, held-out scenarios, labels, manual and manifest) is checked and published with it; when the draft
/// changes a scenario, that manifest is bumped a version for the runner to seal.
/// </summary>
/// <remarks>
/// The studio's one bridge from records to files: check, try and publish all read the folder this writes, so what is
/// checked is what is published. The eval's folder is copied, never read: the runner stays the one reader of eval files
/// (finding R). The draft's files go over the copy, so a file the draft renamed leaves its old one beside it, as an
/// edit in Rider would until the person deletes it. The scenarios and the manifest are written with the scenario
/// files' own wire (<see cref="SampleBody.FileJson"/>), snake_case and indented.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0058-an-eval-is-a-folder-of-files.md">ADR 0058: an eval is a folder of files</seealso>
public static class DraftFolder
{
    /// <summary>How a bumped manifest is written back: indented, as the eval folders keep theirs.</summary>
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>
    /// The draft's folder under <paramref name="root"/>, written afresh, and its path; an edit starts from a copy of
    /// <paramref name="existing"/>, the eval's folder in the workspace (<see cref="EvalHomes.Existing"/>).
    /// </summary>
    public static string Write(EvalDraft draft, EvalDrafts drafts, string root, string? existing)
    {
        string folder = Path.Combine(root, draft.Id);
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);

        // An edit of an eval the workspace has starts from its folder (FX32): its references, held-out scenarios,
        // labels, manual and manifest stay, and the draft's files go over them.
        if (existing is not null) Copy(existing, folder);

        File.WriteAllText(Path.Combine(folder, "eval.json"), draft.Json);
        foreach (var rubric in draft.Rubrics) Write(folder, ["Rubrics", rubric.Name], rubric.Text);

        // The dataset: a new eval's manifest for the runner to seal (an edit keeps its own), the draft's own scenarios
        // and the sample drafts as dev scenarios.
        var manifest = new ManifestFile(draft.Name, 1, MessageText.Format(DraftMessage.ComposedCovers), "",
            [MessageText.Format(DraftMessage.ComposedChange, new { Date = $"{draft.At:yyyy-MM-dd}" })]);
        if (!File.Exists(Path.Combine(folder, "Dataset", "dataset.json")))
            Write(folder, ["Dataset", "dataset.json"], JsonSerializer.Serialize(manifest, SampleBody.FileJson) + "\n");
        bool changed = false;
        foreach (var scenario in draft.Scenarios)
            changed |= WriteScenario(folder, scenario.Name, scenario.Text);
        foreach (var sample in drafts.Samples)
            changed |= WriteScenario(folder, sample.Sample.Id + ".json",
                JsonSerializer.Serialize(sample.Sample, SampleBody.FileJson) + "\n");

        // An edit that changed a scenario bumps the eval's own manifest (FX32), as a person does by hand.
        if (existing is not null && changed) Bump(Path.Combine(folder, "Dataset", "dataset.json"), draft.At);

        // The kept transcripts, one protocol line each, as the eval folders hold theirs.
        foreach (var transcript in drafts.Transcripts)
            Write(folder, ["Transcripts", transcript.FileName],
                string.Concat(transcript.Lines.Select(line => line + "\n")));
        return folder;
    }

    /// <summary>Copies every file of the eval's folder into the draft's, keeping their paths.</summary>
    private static void Copy(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    /// <summary>Writes one dev scenario, and whether that changed it: a new file, or other text than before.</summary>
    private static bool WriteScenario(string folder, string name, string text)
    {
        string file = Path.Combine(folder, "Dataset", "scenarios", "dev", name);
        bool same = File.Exists(file) && File.ReadAllText(file) == text;
        Write(folder, ["Dataset", "scenarios", "dev", name], text);
        return !same;
    }

    /// <summary>
    /// Bumps a manifest whose scenarios changed: one version up, a dated change line, and its hash cleared for the
    /// runner to seal (<c>ManifestSeal</c>); the rest is kept as written.
    /// </summary>
    /// <remarks>
    /// Read as a <see cref="JsonObject"/> to change three fields in place, as the runner's seal does (ADR 0041: a node
    /// for an edit in place). A manifest that isn't an object is left for the runner's check to name.
    /// </remarks>
    private static void Bump(string manifest, DateTimeOffset at)
    {
        if (JsonNode.Parse(File.ReadAllText(manifest)) is not JsonObject node) return;
        node["version"] = (node["version"]?.GetValue<int>() ?? 0) + 1;
        if (node["changes"] is not JsonArray changes) node["changes"] = changes = [];
        changes.Add(MessageText.Format(DraftMessage.EditedChange, new { Date = $"{at:yyyy-MM-dd}" }));
        node["content_hash"] = "";
        File.WriteAllText(manifest, node.ToJsonString(Indented) + "\n");
    }

    /// <summary>Writes one file under the folder, making its folders first.</summary>
    private static void Write(string folder, string[] path, string text)
    {
        string file = Path.Combine([folder, .. path]);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }

    /// <summary>A dataset's manifest as the eval folders write it, its hash empty until the runner seals it.</summary>
    private sealed record ManifestFile(
        string Name,
        int Version,
        string Covers,
        string ContentHash,
        IReadOnlyList<string> Changes);
}
