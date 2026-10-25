using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Messages;

namespace LouisAgent.Core.Tests.Messages;

// CL3: the texts tools return to the model (errors, timeouts, what to do instead) are part of what the model reads, like
// the tool descriptions in ModelRequestSnapshotTests. Every *ToolMessage enum (the shared one and each area's, the split by area, F18:
// the split by area) is rendered with a fixed sample per hole ({Path} becomes <Path>), keyed by the member's bare name, and
// compared with a committed golden file, so a wording change is deliberate and reviewed. Member names are unique across them
// (a test below holds that), so the keys don't depend on which enum a text lives in. To change one: set LOUIS_UPDATE_GOLDEN=1,
// run this test, review the file's diff, commit it.
public class ToolMessageSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.core.tests", "Messages", "ToolMessageSnapshot.json");

    private static readonly Type[] ToolMessages = typeof(MessageText).Assembly.GetTypes()
        .Where(t => t.IsEnum && t.Name.EndsWith("ToolMessage", StringComparison.Ordinal) && t.Namespace?.Split('.').Last() == "Messages")
        .ToArray();

    private static string Snapshot()
    {
        var rendered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (Enum message in ToolMessages.SelectMany(t => Enum.GetValues(t).Cast<Enum>()))
        {
            string text = MessageText.Template(message);
            foreach (string hole in MessageText.Holes(text)) text = text.Replace($"{{{hole}}}", $"<{hole}>");
            // Add, not the indexer: a name in two enums would overwrite the other's text without a sound.
            rendered.Add(message.ToString(), text.ReplaceLineEndings("\n"));
        }

        return JsonSerializer.Serialize(rendered, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }).ReplaceLineEndings("\n") + "\n";
    }

    [Test]
    public void NoTwoToolMessageEnums_ShareAMemberName()
    {
        // The golden file keys each text by its member's name, so two enums with one name would have one text in the file.
        var owners = ToolMessages
            .SelectMany(t => Enum.GetNames(t).Select(name => (Name: name, Enum: t.Name)))
            .GroupBy(m => m.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(m => m.Enum))}")
            .ToList();

        Assert.That(owners, Is.Empty, "These member names appear in more than one *ToolMessage enum:\n  " + string.Join("\n  ", owners));
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