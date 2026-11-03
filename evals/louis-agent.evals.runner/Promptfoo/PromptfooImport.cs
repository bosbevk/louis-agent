using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Messages;
using YamlDotNet.RepresentationModel;

namespace LouisAgent.Evals.Runner.Promptfoo;

/// <summary>
/// Turns a promptfoo file into an eval folder (F49.5, finding T): its tests are the scenarios, the asserts every test
/// shares (<c>defaultTest</c>) are the eval's checks by the mapping table, a test's own <c>contains</c> is that
/// scenario's expected value read by <c>mentions-expected</c>, an <c>llm-rubric</c> is a judge with its rubric file.
/// What has no counterpart is handed back by name, never dropped unsaid.
/// </summary>
/// <remarks>
/// Adapter (Gang of Four) between promptfoo's shape and ours, with the table as <see cref="PromptfooAssertion"/>. The
/// YAML is read as YamlDotNet's node model, since a value may be text or a list. The folder is written unsealed and
/// with one <c>imported</c> mode, for the studio to finish: the runner seals the dataset, the person sets the modes.
/// </remarks>
public static class PromptfooImport
{
    /// <summary>Writes the folder and hands back what was left out, one line each.</summary>
    public static IReadOnlyList<string> Write(string yaml, string folder)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        if (stream.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode root)
            throw new RunnerException(MessageText.Format(RunnerMessage.PromptfooNotAFile));

        var skipped = new List<string>();
        var checks = SharedChecks(Sequence(Mapping(root, "defaultTest"), "assert"), folder, skipped);
        string variable = VariableOf(Sequence(root, "prompts").FirstOrDefault());
        var tests = Sequence(root, "tests").OfType<YamlMappingNode>().ToList();
        bool expectsContains = false;
        for (int i = 0; i < tests.Count; i++)
            expectsContains |= WriteScenario(tests[i], i + 1, variable, folder, skipped);
        if (expectsContains) checks.Add(Check("mentions-expected", new JsonObject { ["key"] = "contains" }));

        string name = Slug(Text(root, "description")) ?? "imported-eval";
        var file = new JsonObject
        {
            ["schema_version"] = 1,
            ["name"] = name,
            ["identity"] = "imported",
            ["version"] = 1,
            ["proves"] = Text(root, "description") ?? MessageText.Format(RunnerMessage.PromptfooImported),
            ["modes"] = new JsonArray(new JsonObject
            {
                ["name"] = "imported",
                ["role"] = "",
                ["toolsets"] = new JsonArray()
            }),
            ["checks"] = new JsonArray([.. checks])
        };
        WriteFile(folder, "eval.json", file);
        WriteFile(folder, "Dataset/dataset.json", new JsonObject
        {
            ["name"] = name,
            ["version"] = 1,
            ["covers"] = MessageText.Format(RunnerMessage.PromptfooImported),
            ["content_hash"] = "",
            ["changes"] = new JsonArray(MessageText.Format(RunnerMessage.PromptfooImportedChange,
                new { Date = $"{DateTime.UtcNow:yyyy-MM-dd}" }))
        });
        return skipped;
    }

    /// <summary>The shared asserts as checks, by the table; one check per type, a value list merged into it.</summary>
    private static List<JsonNode> SharedChecks(IEnumerable<YamlNode> asserts, string folder, List<string> skipped)
    {
        var texts = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
        var checks = new List<JsonNode>();
        int rubrics = 0;
        foreach (var assert in asserts.OfType<YamlMappingNode>())
        {
            string type = Text(assert, "type") ?? "";
            string value = Text(assert, "value") ?? "";
            if (!SettingChoice.TryParse(type, out PromptfooAssertion? kind) || kind is null)
            {
                skipped.Add(MessageText.Format(RunnerMessage.PromptfooLeftOut, new { Type = type }));
                continue;
            }

            switch (kind)
            {
                case PromptfooAssertion.Contains or PromptfooAssertion.IContains:
                    Add(texts, checks, "contains", value);
                    break;
                case PromptfooAssertion.NotContains or PromptfooAssertion.NotIContains:
                    Add(texts, checks, "not-contains", value);
                    break;
                case PromptfooAssertion.StartsWith:
                    checks.Add(Check("starts-with", new JsonObject { ["text"] = value }));
                    break;
                case PromptfooAssertion.IsJson:
                    checks.Add(Check("is-json", null));
                    break;
                case PromptfooAssertion.Levenshtein:
                    checks.Add(Check("near", new JsonObject
                    {
                        ["text"] = value,
                        ["distance"] = (int)Number(assert, "threshold", 5)
                    }));
                    break;
                case PromptfooAssertion.Latency:
                    checks.Add(Check("within-time", new JsonObject
                    {
                        ["seconds"] = (int)Math.Ceiling(Number(assert, "threshold", 60000) / 1000)
                    }));
                    break;
                case PromptfooAssertion.Cost:
                    checks.Add(Check("cost-under", new JsonObject { ["limit_usd"] = Number(assert, "threshold", 1) }));
                    break;
                case PromptfooAssertion.LlmRubric:
                    rubrics++;
                    string rubric = $"Rubrics/rubric-{rubrics}.md";
                    Directory.CreateDirectory(Path.Combine(folder, "Rubrics"));
                    File.WriteAllText(Path.Combine(folder, rubric), value);
                    var judge = Check("judge", new JsonObject { ["rubric"] = rubric });
                    if (rubrics > 1) judge["name"] = $"judge-{rubrics}";
                    checks.Add(judge);
                    break;
            }
        }

        return checks;
    }

    /// <summary>One test as a dev scenario; whether it set a <c>contains</c> expected value.</summary>
    private static bool WriteScenario(YamlMappingNode test, int number, string variable, string folder,
        List<string> skipped)
    {
        string id = Slug(Text(test, "description")) ?? $"test-{number}";
        var vars = Mapping(test, "vars");
        string question = (vars is null ? null : Text(vars, variable)) ?? "";
        var expected = new JsonObject();
        foreach (var assert in Sequence(test, "assert").OfType<YamlMappingNode>())
        {
            string type = Text(assert, "type") ?? "";
            if (type is "contains" or "icontains" && !expected.ContainsKey("contains"))
                expected["contains"] = Text(assert, "value") ?? "";
            else
                skipped.Add(MessageText.Format(RunnerMessage.PromptfooTestLeftOut, new { Type = type, Test = id }));
        }

        WriteFile(folder, $"Dataset/scenarios/dev/{id}.json", new JsonObject
        {
            ["id"] = id,
            ["input"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["content"] = question
            }),
            ["metadata"] = new JsonObject
            {
                ["place"] = null,
                ["time_zone"] = "Africa/Johannesburg",
                ["follow_ups"] = new JsonArray(),
                ["expected"] = expected,
                ["categories"] = new JsonArray()
            }
        });
        return expected.ContainsKey("contains");
    }

    /// <summary>Adds a text to its type's one check, making the check the first time.</summary>
    private static void Add(Dictionary<string, JsonArray> texts, List<JsonNode> checks, string type, string value)
    {
        if (!texts.TryGetValue(type, out var list))
        {
            texts[type] = list = [];
            checks.Add(Check(type, new JsonObject { ["texts"] = list }));
        }

        list.Add(value);
    }

    /// <summary>A check's part of the file.</summary>
    private static JsonObject Check(string type, JsonObject? parameters)
    {
        var check = new JsonObject { ["type"] = type };
        if (parameters is not null) check["parameters"] = parameters;
        return check;
    }

    /// <summary>The prompt's variable, the first <c>{{name}}</c> in it; <c>question</c> when it names none.</summary>
    private static string VariableOf(YamlNode? prompt)
    {
        string text = (prompt as YamlScalarNode)?.Value ?? "";
        int start = text.IndexOf("{{", StringComparison.Ordinal), end = text.IndexOf("}}", StringComparison.Ordinal);
        return start >= 0 && end > start ? text[(start + 2)..end].Trim() : "question";
    }

    /// <summary>A name in kebab-case: lowercase letters and digits, the rest one dash; none for no letters.</summary>
    internal static string? Slug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var chars = text.Trim().ToLowerInvariant().Select(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        string slug = string.Join("-", new string([.. chars]).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length == 0 ? null : slug[..Math.Min(slug.Length, 80)].Trim('-');
    }

    /// <summary>A child's text, if it is a scalar.</summary>
    private static string? Text(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out var child) ? (child as YamlScalarNode)?.Value : null;

    /// <summary>A child's number, or the default when it has none.</summary>
    private static double Number(YamlMappingNode node, string key, double fallback) =>
        double.TryParse(Text(node, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? n : fallback;

    /// <summary>A child mapping, if there is one.</summary>
    private static YamlMappingNode? Mapping(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out var child) ? child as YamlMappingNode : null;

    /// <summary>A child list's items; none when it isn't there.</summary>
    private static IEnumerable<YamlNode> Sequence(YamlMappingNode? node, string key) =>
        node is not null && node.Children.TryGetValue(new YamlScalarNode(key), out var child) &&
        child is YamlSequenceNode list
            ? list.Children
            : [];

    /// <summary>Writes one JSON file under the folder, indented, its folders made first.</summary>
    private static void WriteFile(string folder, string path, JsonNode json)
    {
        string file = Path.Combine(folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}
