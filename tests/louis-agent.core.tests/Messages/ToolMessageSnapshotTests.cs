using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Messages;

namespace LouisAgent.Core.Tests.Messages;

// CL3: the texts tools return to the model (errors, timeouts, what to do instead) are part of what the model reads, like
// the tool descriptions in ModelRequestSnapshotTests. Every ToolMessage is rendered with a fixed sample per hole ({Path}
// becomes <Path>) and compared with a committed golden file, so a wording change is deliberate and reviewed. To change
// one: set LOUIS_UPDATE_GOLDEN=1, run this test, review the file's diff, commit it.
public class ToolMessageSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.core.tests", "Messages", "ToolMessageSnapshot.json");

    private static string Snapshot()
    {
        var rendered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (ToolMessage message in Enum.GetValues<ToolMessage>())
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

    [Test]
    public void WhatToolsTellTheModel_MatchesTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")),
            "A text tools return to the model changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }
}