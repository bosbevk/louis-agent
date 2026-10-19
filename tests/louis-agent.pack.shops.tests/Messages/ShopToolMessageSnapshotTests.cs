using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.Shops.Messages;

namespace LouisAgent.Packs.Shops.Tests.Messages;

// CL15 step 4: the texts the shop tools return to the model left core's ToolMessage with the pack, and so did their
// golden: every ShopToolMessage rendered with a fixed sample per hole, compared with a committed file, byte-equal to the
// seventeen entries core's ToolMessageSnapshot.json lost. To change one: LOUIS_UPDATE_GOLDEN=1, run, review the diff, commit.
public class ShopToolMessageSnapshotTests
{
    private static readonly string GoldenFile =
        Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.pack.shops.tests", "Messages", "ShopToolMessageSnapshot.json");

    private static string Snapshot()
    {
        var rendered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (ShopToolMessage message in Enum.GetValues<ShopToolMessage>())
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
    public void WhatTheShopToolsTellTheModel_MatchesTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")),
            "A text the shop tools return to the model changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [Test]
    public void EveryMember_HasATemplate_WithNoStrayBrace()
    {
        foreach (var message in Enum.GetValues<ShopToolMessage>().Cast<Enum>().Concat(Enum.GetValues<ShopLogMessage>().Cast<Enum>()))
        {
            string text = MessageText.Template(message);
            foreach (string hole in MessageText.Holes(text)) text = text.Replace($"{{{hole}}}", "");
            Assert.That(text, Does.Not.Contain("{").And.Not.Contain("}"), $"{message}: a hole that isn't a name");
        }
    }
}
