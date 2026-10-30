using LouisAgent.Testing;
using LouisAgent.Evals.Datasets.Manifest;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// A copied eval-files root in a temp directory, holding one sample eval folder at a path as the repository has it:
/// an <c>eval.json</c> with one general check, a one-scenario dataset and a manual. Removed when disposed.
/// </summary>
internal sealed class SampleEvalFolder : IDisposable
{
    /// <summary>The sample eval's file: one mode, and the general no-error check as its required fixed rule.</summary>
    private const string EvalFile =
        $$"""
          {
            "schema_version": 1,
            "name": "{{SampleEval.Alpha}}",
            "identity": "{{SampleIdentity.Alpha}}",
            "version": 1,
            "proves": "The agent answers.",
            "manual": "manual.md",
            "modes": [ { "name": "{{SampleMode.Alpha}}", "role": "{{SampleRole.Alpha}}", "toolsets": ["{{SampleToolset.Alpha}}"] } ],
            "checks": [ { "type": "no-error" } ]
          }
          """;

    /// <summary>One scenario in Inspect's Sample shape, expecting nothing a check reads.</summary>
    private const string Scenario =
        """
        {"id": "hello", "input": [{"role": "user", "content": "Hello"}],
         "metadata": {"place": null, "time_zone": "Etc/UTC", "follow_ups": [], "expected": {}, "categories": []}}
        """;

    /// <summary>The copied root.</summary>
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "eval-files-" + Guid.NewGuid().ToString("N"));

    /// <summary>Writes the sample eval at <paramref name="source"/>, a path from the repository root.</summary>
    public SampleEvalFolder(string source)
    {
        Write($"{source}/eval.json", EvalFile);
        Write($"{source}/manual.md", "# Sample\n");
        Write($"{source}/Dataset/scenarios/dev/hello.json", Scenario);
        var hash = DatasetHash.Of(Path.Combine(Root, source, "Dataset", "scenarios"));
        Write($"{source}/Dataset/dataset.json",
            "{\"name\": \"sample-dataset\", \"version\": 1, \"covers\": \"A sample task\", \"content_hash\": \"" + hash
            + "\", \"changes\": [\"2026-10-30: a sample change\"]}");
    }

    /// <summary>Writes a file at a path relative to the root, with forward slashes.</summary>
    public void Write(string relative, string content)
    {
        var file = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }

    /// <summary>Removes the root.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
