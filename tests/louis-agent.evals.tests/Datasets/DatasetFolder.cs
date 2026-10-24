using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Tests.Datasets;

/// <summary>A dataset folder in a temp directory, written file by file and removed when disposed.</summary>
internal sealed class DatasetFolder : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "dataset-" + Guid.NewGuid().ToString("N"));

    public DatasetFolder() => Directory.CreateDirectory(Root);

    /// <summary>Writes a file at a path relative to the folder, with forward slashes.</summary>
    public DatasetFolder Write(string relative, string content)
    {
        string file = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
        return this;
    }

    /// <summary>Writes <c>dataset.json</c> with the scenarios' current hash, so a test breaks only what it means to break.</summary>
    public DatasetFolder WithManifest(int version = 1) =>
        Write("dataset.json", "{\"name\": \"puls-cook\", \"version\": " + version + ", \"covers\": \"Cooking meals\", \"content_hash\": \""
            + DatasetHash.Of(Path.Combine(Root, "scenarios")) + "\", \"changes\": [\"2026-10-24: first version\"]}");

    /// <summary>One scenario file's text: the input is fixed, the expected and categories are the test's.</summary>
    public static string Scenario(string id, string expected = "{\"routed\": true}", string categories = "[]", string extra = "") =>
        "{\"id\": \"" + id + "\", \"input\": {\"place\": \"Pretoria\", \"time_zone\": \"Africa/Johannesburg\", \"messages\": [\"Gee my resep\"]},"
        + " \"expected\": " + expected + ", \"categories\": " + categories + extra + "}";

    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
