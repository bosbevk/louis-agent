using LouisAgent.Choices;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// The guide teaches each kind of picture with a whole example (F134.7). Someone copies those, so each one must be
/// a file the reader really reads, and there must be one for every kind the tool draws.
/// </summary>
public class GuideExamplesTests
{
    /// <summary>The guide, as it is in the repo.</summary>
    private static string Guide =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/docs/guides/draw-a-diagram.md"))
            .ReplaceLineEndings("\n");

    /// <summary>
    /// Every JSON example in the guide: what stands between a json fence and the fence that closes it.
    /// </summary>
    private static List<string> Examples() =>
    [
        .. Guide.Split("```json\n").Skip(1).Select(after => after[..after.IndexOf("\n```", StringComparison.Ordinal)])
            .Where(example => example.Contains("\"kind\"", StringComparison.Ordinal))
    ];

    /// <summary>Every example is a picture the reader reads, with nothing misspelt and nothing left out.</summary>
    [Test]
    public void EveryExample_IsOneTheReaderReads()
    {
        var unread = Examples().Where(example => Refusal(example) is not null)
            .Select(example => $"{Refusal(example)}: {example[..Math.Min(example.Length, 60)]}");

        Assert.That(unread, Is.Empty);
    }

    /// <summary>There is an example of every kind the tool draws, so no kind is left to guesswork.</summary>
    [Test]
    public void EveryKind_HasItsExample()
    {
        var shown = Examples().Where(example => Refusal(example) is null)
            .Select(example => DiagramReader.Read(example).Kind.Wire()).Distinct();

        Assert.That(shown, Is.EquivalentTo(SettingChoice.Names<DiagramKind>()));
    }

    /// <summary>Why the reader refuses an example. Nothing when it reads it.</summary>
    private static string? Refusal(string example)
    {
        try
        {
            DiagramReader.Read(example);
            return null;
        }
        catch (InvalidDataException refused)
        {
            return refused.Message;
        }
    }
}
