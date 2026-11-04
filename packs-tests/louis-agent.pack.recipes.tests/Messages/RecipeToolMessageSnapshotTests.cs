using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.Recipes.Messages;

namespace LouisAgent.Packs.Recipes.Tests.Messages;

/// <summary>
/// Pins what the recipe tools say to the model: every <see cref="RecipeToolMessage"/> rendered with a fixed sample
/// for each hole and compared with a committed file, so a changed word is a change somebody reviews. To change one:
/// <c>LOUIS_UPDATE_GOLDEN=1</c>, run, read the diff, commit.
/// </summary>
public class RecipeToolMessageSnapshotTests
{
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot, "packs-tests/louis-agent.pack.recipes.tests", "Messages", "RecipeToolMessageSnapshot.json");

    /// <summary>Every template with each hole shown as its name in angle brackets, as the file's JSON.</summary>
    private static string Snapshot()
    {
        var rendered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var message in Enum.GetValues<RecipeToolMessage>())
        {
            string text = MessageText.Template(message);
            foreach (string hole in MessageText.Holes(text)) text = text.Replace($"{{{hole}}}", $"<{hole}>");
            rendered[message.ToString()] = text.ReplaceLineEndings("\n");
        }

        return JsonSerializer.Serialize(rendered, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>The texts are the ones in the committed file.</summary>
    [Test]
    public void WhatTheRecipeToolsTellTheModel_MatchesTheGoldenFile()
    {
        string actual = Snapshot();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")),
            "A text the recipe tools return to the model changed. If that is intended, rerun with " +
            "LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    /// <summary>Every template, the log and error ones too, has holes that are names and no stray brace.</summary>
    [Test]
    public void EveryMember_HasATemplate_WithNoStrayBrace()
    {
        var messages = Enum.GetValues<RecipeToolMessage>().Cast<Enum>()
            .Concat(Enum.GetValues<RecipeLogMessage>().Cast<Enum>())
            .Concat(Enum.GetValues<RecipeErrorMessage>().Cast<Enum>());
        foreach (var message in messages)
        {
            string text = MessageText.Template(message);
            foreach (string hole in MessageText.Holes(text)) text = text.Replace($"{{{hole}}}", "");
            Assert.That(text, Does.Not.Contain("{").And.Not.Contain("}"), $"{message}: a hole that isn't a name");
        }
    }
}
