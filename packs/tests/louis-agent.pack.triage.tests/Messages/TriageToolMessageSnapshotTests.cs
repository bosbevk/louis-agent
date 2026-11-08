using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages;

namespace LouisAgent.Packs.Triage.Tests.Messages;

/// <summary>
/// What the triage pack tells the model is pinned: every text rendered with its holes named, compared with a
/// committed file. To change one: LOUIS_UPDATE_GOLDEN=1, run, read the diff, commit.
/// </summary>
public class TriageToolMessageSnapshotTests
{
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot,
        "packs/tests/louis-agent.pack.triage.tests", "Messages", "TriageToolMessageSnapshot.json");

    /// <summary>Every member's text, its holes shown as names.</summary>
    private static string Snapshot()
    {
        var rendered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var message in Enum.GetValues<TriageToolMessage>())
        {
            string text = MessageText.Template(message);
            foreach (string hole in MessageText.Holes(text)) text = text.Replace($"{{{hole}}}", $"<{hole}>");
            rendered[message.ToString()] = text.ReplaceLineEndings("\n");
        }

        return JsonSerializer.Serialize(rendered, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>The texts are the committed ones, or the run writes them when asked to.</summary>
    [Test]
    public void WhatTheToolTellsTheModel_MatchesTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")),
            "A text the triage pack returns to the model changed. If that is intended, rerun with " +
            "LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }
}
