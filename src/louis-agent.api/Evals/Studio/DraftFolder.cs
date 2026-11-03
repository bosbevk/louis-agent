using System.Text.Json;
using LouisAgent.Api.Evals.Drafts;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// Writes an eval being composed out as the folder the runner reads (ADR 0058): its file as typed, its rubrics under
/// <c>Rubrics/</c>, a manifest for its dataset with the hash left for the runner to seal, the eval's sample drafts as
/// its dev scenarios and its transcript drafts under the names they were kept with. Each draft has a folder of its own
/// under the root, written afresh every time, so a file the draft dropped is gone.
/// </summary>
/// <remarks>
/// The studio's one bridge from records to files: check, try and publish all read the folder this writes, so what is
/// checked is what is published. The scenarios and the manifest are written with the scenario files' own wire
/// (<see cref="SampleBody.FileJson"/>), snake_case and indented.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0058-an-eval-is-a-folder-of-files.md">ADR 0058: an eval is a folder of files</seealso>
public static class DraftFolder
{
    /// <summary>The draft's folder under <paramref name="root"/>, written afresh, and its path.</summary>
    public static string Write(EvalDraft draft, EvalDrafts drafts, string root)
    {
        string folder = Path.Combine(root, draft.Id);
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);

        File.WriteAllText(Path.Combine(folder, "eval.json"), draft.Json);
        foreach (var rubric in draft.Rubrics) Write(folder, ["Rubrics", rubric.Name], rubric.Text);

        // The dataset: a manifest the runner seals, the draft's own scenarios and the sample drafts as dev scenarios.
        var manifest = new ManifestFile(draft.Name, 1, MessageText.Format(DraftMessage.ComposedCovers), "",
            [MessageText.Format(DraftMessage.ComposedChange, new { Date = $"{draft.At:yyyy-MM-dd}" })]);
        Write(folder, ["Dataset", "dataset.json"], JsonSerializer.Serialize(manifest, SampleBody.FileJson) + "\n");
        foreach (var scenario in draft.Scenarios)
            Write(folder, ["Dataset", "scenarios", "dev", scenario.Name], scenario.Text);
        foreach (var sample in drafts.Samples)
            Write(folder, ["Dataset", "scenarios", "dev", sample.Sample.Id + ".json"],
                JsonSerializer.Serialize(sample.Sample, SampleBody.FileJson) + "\n");

        // The kept transcripts, one protocol line each, as the eval folders hold theirs.
        foreach (var transcript in drafts.Transcripts)
            Write(folder, ["Transcripts", transcript.FileName],
                string.Concat(transcript.Lines.Select(line => line + "\n")));
        return folder;
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
