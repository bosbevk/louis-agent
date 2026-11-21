using LouisAgent.Evals.Datasets.Manifest;

namespace LouisAgent.Evals.Tests.Datasets;

/// <summary>A dataset folder in a temp directory, written file by file and removed when disposed.</summary>
internal sealed class DatasetFolder : IDisposable
{
    /// <summary>Where the folder is.</summary>
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "dataset-" + Guid.NewGuid().ToString("N"));

    /// <summary>Makes the empty folder.</summary>
    public DatasetFolder() => Directory.CreateDirectory(Root);

    /// <summary>Writes a file at a path relative to the folder, with forward slashes.</summary>
    public DatasetFolder Write(string relative, string content)
    {
        string file = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
        return this;
    }

    /// <summary>
    /// Writes <c>dataset.json</c> with the scenarios' current hash and <paramref name="lines"/> dated change lines, so
    /// a test breaks only what it means to break.
    /// </summary>
    public DatasetFolder WithManifest(int version = 1, int lines = 1)
    {
        string hash = DatasetHash.Of(Path.Combine(Root, "scenarios"));
        string changes = string.Join(", ", Enumerable.Repeat("\"2026-10-24: a sample change\"", lines));
        return Write("dataset.json", "{\"name\": \"sample-eval\", \"version\": " + version +
                                     ", \"covers\": \"A sample task\", \"content_hash\": \""
                                     + hash + "\", \"changes\": [" + changes + "]}");
    }

    /// <summary>
    /// One scenario file's text in Inspect's Sample shape (F18 step 7d): a sample question from the user, and under
    /// <c>metadata</c> a sample place and time zone with the test's expected, categories and follow-ups. Anything in
    /// <paramref name="extra"/> lands at the top level.
    /// </summary>
    public static string Scenario(string id, string expected = "{\"routed\": true}", string categories = "[]",
        string extra = "", string followUps = "[]", string target = "") =>
        "{\"id\": \"" + id + "\", \"input\": [{\"role\": \"user\", \"content\": \"Hello\"}]" + target
        + ", \"metadata\": {\"place\": \"Sample town\", \"time_zone\": \"Etc/UTC\", \"follow_ups\": " + followUps
        + ", \"expected\": " + expected + ", \"categories\": " + categories + "}" + extra + "}";

    /// <summary>Removes the folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
