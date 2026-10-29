using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner.Samples;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Tests.Samples;

/// <summary>
/// The one-off <c>convert-samples</c> command (F18 step 7d): a dataset in the old scenario shape becomes Inspect's
/// Sample shape, the loader reads it, and its manifest goes one version on with a dated line and the new hash.
/// </summary>
public class SampleConversionTests
{
    /// <summary>A dataset folder in the old shape: two scenarios, one with a follow-up, and a first manifest.</summary>
    private static string OldDataset()
    {
        string root = Path.Combine(Path.GetTempPath(), "samples-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenarios", "dev"));
        Directory.CreateDirectory(Path.Combine(root, "scenarios", "held-out"));
        File.WriteAllText(Path.Combine(root, "scenarios", "dev", "cook.json"),
            "{\"id\": \"cook\", \"input\": {\"place\": \"Stellenbosch\", \"time_zone\": \"Africa/Johannesburg\", "
            + "\"messages\": [\"Bobotie vir vier, asseblief.\", \"En vir ses?\"]}, "
            + "\"expected\": {\"routed\": \"cook\"}, \"categories\": [\"language:af\", \"follow-up\"]}");
        File.WriteAllText(Path.Combine(root, "scenarios", "held-out", "news.json"),
            "{\"id\": \"news\", \"input\": {\"place\": null, \"time_zone\": \"Etc/UTC\", "
            + "\"messages\": [\"What's new?\"]}, \"expected\": {\"routed\": null}}");
        string hash = DatasetHash.Of(Path.Combine(root, "scenarios"));
        File.WriteAllText(Path.Combine(root, "dataset.json"), "{\"name\": \"sample-eval\", \"version\": 1, "
                                                              + "\"covers\": \"A sample task\", \"content_hash\": \"" +
                                                              hash + "\", "
                                                              + "\"changes\": [\"2026-10-24: first version\"]}");
        return root;
    }

    /// <summary>After the conversion the loader reads the dataset, its messages and criteria as they were.</summary>
    [Test]
    public void AConvertedDataset_Loads_WithItsQuestionsInOrder()
    {
        string root = OldDataset();

        int converted = SampleConversion.Convert(root, new DateOnly(2026, 10, 29));

        Dataset dataset = DatasetLoader.Load(root, [new ReadsKeys("routed")]);
        Assert.That(converted, Is.EqualTo(2));
        var cook = dataset.Scenarios.Single(s => s.Id == "cook");
        Assert.That(cook.Input.Messages, Is.EqualTo(new[] { "Bobotie vir vier, asseblief.", "En vir ses?" }));
        Assert.That((cook.Input.Place, cook.Input.TimeZone), Is.EqualTo(("Stellenbosch", "Africa/Johannesburg")));
        Assert.That(cook.Categories, Is.EqualTo(new[] { "language:af", "follow-up" }));
        Assert.That(cook.Expected.GetProperty("routed").GetString(), Is.EqualTo("cook"));
        Assert.That(dataset.Scenarios.Single(s => s.Id == "news").Input.Place, Is.Null);
    }

    /// <summary>The manifest goes one version on, with the day's line last and the converted files' hash.</summary>
    [Test]
    public void TheManifest_GoesOneVersionOn_WithADatedLineAndTheNewHash()
    {
        string root = OldDataset();

        SampleConversion.Convert(root, new DateOnly(2026, 10, 29));

        var manifest = DatasetLoader.Load(root, [new ReadsKeys("routed")]).Manifest;
        Assert.That(manifest.Version, Is.EqualTo(2));
        Assert.That(manifest.Changes[^1], Does.StartWith("2026-10-29: step 7d - "));
        Assert.That(manifest.ContentHash, Is.EqualTo(DatasetHash.Of(Path.Combine(root, "scenarios"))));
        Assert.That((manifest.Name, manifest.Covers), Is.EqualTo(("sample-eval", "A sample task")));
    }

    /// <summary>
    /// The file reads as a person would write it: the user's role named, letters such as ê kept as they are.
    /// </summary>
    [Test]
    public void TheFile_NamesTheUsersRole_AndKeepsItsLetters()
    {
        string root = OldDataset();
        File.WriteAllText(Path.Combine(root, "scenarios", "dev", "cook.json"),
            "{\"id\": \"cook\", \"input\": {\"place\": \"Paarl\", \"time_zone\": \"Africa/Johannesburg\", "
            + "\"messages\": [\"Wat kos 'n ê?\"]}, \"expected\": {\"routed\": \"cook\"}}");

        SampleConversion.Convert(root, new DateOnly(2026, 10, 29));

        string text = File.ReadAllText(Path.Combine(root, "scenarios", "dev", "cook.json"));
        Assert.That(text, Does.Contain("\"role\": \"user\"").And.Contain("Wat kos 'n ê?"));
        Assert.That(text, Does.Contain("\"follow_ups\": []").And.Not.Contain("\"target\""));
    }

    /// <summary>A grader named only for the keys it reads; it never grades.</summary>
    private sealed class ReadsKeys(params string[] keys) : IGrader
    {
        /// <summary>Its name.</summary>
        public string Name => "reads-keys";

        /// <summary>Its version.</summary>
        public int Version => 1;

        /// <summary>Its kind.</summary>
        public GraderKind Kind => GraderKind.FixedRule;

        /// <summary>It is required.</summary>
        public bool Required => true;

        /// <summary>The keys it reads.</summary>
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>(keys);

        /// <summary>Never called: the loader only reads its keys.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
