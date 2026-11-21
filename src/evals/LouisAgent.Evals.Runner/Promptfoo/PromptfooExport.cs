using System.Globalization;
using System.Text.Json.Nodes;
using LouisAgent.Choices;
using YamlDotNet.Serialization;

namespace LouisAgent.Evals.Runner.Promptfoo;

/// <summary>
/// Writes an eval folder out as a promptfoo file (F49.5, finding T): its scenarios are the tests, each asking its
/// first question as <c>question</c>; its checks with a counterpart are the shared asserts, by the mapping table
/// backwards; <c>mentions-expected</c> is each test's own <c>contains</c> of its expected value. A check with no
/// counterpart is named in a comment at the top, never dropped unsaid.
/// </summary>
/// <remarks>
/// The other half of <see cref="PromptfooImport"/>'s Adapter. It reads the folder's files as written (the runner's
/// strict load is for running it, and an export may be of a half-made draft), and builds the YAML as plain lists and
/// maps for YamlDotNet's serializer.
/// </remarks>
public static class PromptfooExport
{
    /// <summary>The folder as a promptfoo file.</summary>
    public static string Yaml(string folder)
    {
        var file = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "eval.json")))!.AsObject();
        var shared = new List<object>();
        var perTest = new List<string>();
        var left = new List<string>();
        foreach (var check in (file["checks"] as JsonArray ?? []).OfType<JsonObject>())
            Map(check, folder, shared, perTest, left);

        var tests = Scenarios(folder).Select(scenario => Test(scenario, perTest)).ToList<object>();
        var promptfoo = new Dictionary<string, object>
        {
            ["description"] = Text(file["proves"]) ?? Text(file["name"]) ?? "",
            ["prompts"] = new List<string> { "{{question}}" },
            ["defaultTest"] = new Dictionary<string, object> { ["assert"] = shared },
            ["tests"] = tests
        };

        string yaml = new SerializerBuilder().Build().Serialize(promptfoo);
        string header = string.Concat(left.Select(type => $"# not exported, no promptfoo counterpart: {type}\n"));
        return header + yaml;
    }

    /// <summary>One check as shared asserts, a per-test key, or a type left out.</summary>
    /// <remarks>A switch on the check's type, one arm per row of the table (ADR 0051).</remarks>
    private static void Map(JsonObject check, string folder, List<object> shared, List<string> perTest,
        List<string> left)
    {
        string type = Text(check["type"]) ?? "";
        var parameters = check["parameters"] as JsonObject ?? [];
        switch (type)
        {
            case "contains" or "not-contains":
                foreach (var text in (parameters["texts"] as JsonArray ?? []).Select(Text))
                    shared.Add(Assert(type, text, null));
                break;
            case "starts-with":
                shared.Add(Assert(type, Text(parameters["text"]), null));
                break;
            case "is-json":
                shared.Add(Assert(type, null, null));
                break;
            case "near":
                shared.Add(Assert(PromptfooAssertion.Levenshtein.Wire(), Text(parameters["text"]),
                    Number(parameters["distance"])));
                break;
            case "within-time":
                shared.Add(Assert(PromptfooAssertion.Latency.Wire(), null, Number(parameters["seconds"]) * 1000));
                break;
            case "cost-under":
                shared.Add(Assert(PromptfooAssertion.Cost.Wire(), null, Number(parameters["limit_usd"])));
                break;
            case "judge":
                string rubric = Path.Combine(folder, Text(parameters["rubric"]) ?? "");
                shared.Add(Assert(PromptfooAssertion.LlmRubric.Wire(),
                    File.Exists(rubric) ? File.ReadAllText(rubric) : Text(parameters["rubric"]), null));
                break;
            case "mentions-expected":
                perTest.Add(Text(parameters["key"]) ?? "");
                break;
            default:
                left.Add(type);
                break;
        }
    }

    /// <summary>A scenario as a test: its id, its first question, and its expected values the checks read.</summary>
    private static Dictionary<string, object> Test(JsonObject scenario, List<string> keys)
    {
        var expected = scenario["metadata"]?["expected"] as JsonObject ?? [];
        var asserts = keys.Where(expected.ContainsKey)
            .Select(key => (object)Assert("contains", Text(expected[key]), null)).ToList();
        var test = new Dictionary<string, object>
        {
            ["description"] = Text(scenario["id"]) ?? "",
            ["vars"] = new Dictionary<string, object>
            {
                ["question"] = Text((scenario["input"] as JsonArray)?.FirstOrDefault()?["content"]) ?? ""
            }
        };
        if (asserts.Count > 0) test["assert"] = asserts;
        return test;
    }

    /// <summary>The folder's scenarios, dev then held-out, each in its id's order.</summary>
    private static IEnumerable<JsonObject> Scenarios(string folder) =>
        new[] { "dev", "held-out" }
            .Select(split => Path.Combine(folder, "Dataset", "scenarios", split))
            .Where(Directory.Exists)
            .SelectMany(split => Directory.EnumerateFiles(split, "*.json").Order(StringComparer.Ordinal))
            .Select(file => JsonNode.Parse(File.ReadAllText(file))!.AsObject());

    /// <summary>One promptfoo assert: its type, and its value and threshold when it has them.</summary>
    private static Dictionary<string, object> Assert(string type, string? value, double? threshold)
    {
        var assert = new Dictionary<string, object> { ["type"] = type };
        if (value is not null) assert["value"] = value;
        if (threshold is { } t) assert["threshold"] = t;
        return assert;
    }

    /// <summary>A node's value as text: a string as itself, anything else as JSON.</summary>
    private static string? Text(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue(out string? text) => text,
        _ => node.ToJsonString(),
    };

    /// <summary>A node's value as a number, 0 when it isn't one.</summary>
    private static double Number(JsonNode? node) =>
        double.TryParse(node?.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? n : 0;
}
