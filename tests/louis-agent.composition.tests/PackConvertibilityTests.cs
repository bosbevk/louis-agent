using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LouisAgent.Composition;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using ModelContextProtocol.Server;

namespace LouisAgent.Composition.Tests;

// CL15 step 4 (ADR 0028): every pack the composition root can build stays convertible to an MCP server. The test sees
// shapes, not behaviour: each tool's function is built as the engine builds it (AgentCatalog.ToolMethodsOf and
// FunctionOf, from the tool type, so no setting, browser or network), then wrapped by the MCP SDK's own McpServerTool,
// which must keep its name, its input schema and its description; the tool returns text, takes plain data and a
// CancellationToken when it waits, carries an MCP-safe name, and names no identity; the pack's guides are prose in its
// own prompts. What the test can't see (a thrown failure, input read from the session, framing and bounds) is ADR 0028's
// review checklist.
public class PackConvertibilityTests
{
    private const string NameRule = @"^[A-Za-z0-9_-]{1,64}\z";

    [TestCaseSource(nameof(EveryPackKind))]
    public void EveryPack_IsConvertibleToAnMcpServer(PackKind kind)
    {
        IAgentPack pack = Plugins.FromSettings(null, kind.ToString()).Packs.Single();
        Assert.That(Problems(pack), Is.Empty);
    }

    private static IEnumerable<PackKind> EveryPackKind() => Enum.GetValues<PackKind>();

    [Test]
    public void AnUnconvertiblePack_ListsExactlyItsProblems()
    {
        Assert.That(Problems(new FakeUnconvertiblePack()), Is.EquivalentTo(new[]
        {
            "fakebad/FindForPuls: its description names the identity 'Puls'",
            "fakebad/FindForPuls: parameter 'area' names the identity 'Puls'",
            "fakebad/Count: returns Int32, not string, Task<string> or ValueTask<string>",
            "fakebad/Read: parameter 'http' is HttpClient, not plain data",
            "fakebad/Wait: waits but takes no CancellationToken",
            "fakebad/AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ: its name isn't " + NameRule,
            "fakebad: toolsets/fakebad.md holds a '## Skill:' procedure; a pack's guide is prose, a procedure becomes a tool",
            "fakenoguide: toolsets/fakenoguide.md isn't in the pack's prompts",
        }));
    }

    /// <summary>Every way <paramref name="pack"/> breaks the rule, one line each; empty when it is convertible.</summary>
    private static IReadOnlyList<string> Problems(IAgentPack pack)
    {
        var problems = new List<string>();
        foreach (ToolsetDefinition toolset in pack.Toolsets)
        {
            problems.AddRange(GuideProblems(pack, toolset));
            if (toolset.ToolType is null)
            {
                problems.Add($"{toolset.Name}: no tool type, so its tools can't be listed");
                continue;
            }
            foreach (MethodInfo method in AgentCatalog.ToolMethodsOf(toolset))
            {
                var shape = ShapeProblems(toolset.Name, method).ToList();
                problems.AddRange(shape);
                problems.AddRange(DescriptionProblems(toolset.Name, method));
                // The SDK is asked only about a tool whose shape is right: it couldn't build a schema for the others.
                if (shape.Count == 0) problems.AddRange(McpProblems(toolset.Name, method));
            }
        }
        return problems;
    }

    private static IEnumerable<string> GuideProblems(IAgentPack pack, ToolsetDefinition toolset)
    {
        if (toolset.GuideFile is null)
        {
            yield return $"{toolset.Name}: no guide";
            yield break;
        }
        IFileInfo guide = pack.Prompts.GetFileInfo(toolset.GuideFile);
        if (!guide.Exists)
        {
            yield return $"{toolset.Name}: {toolset.GuideFile} isn't in the pack's prompts";
            yield break;
        }
        using var reader = new StreamReader(guide.CreateReadStream());
        string text = reader.ReadToEnd();
        if (text.Split('\n').Any(line => line.TrimEnd().StartsWith("## Skill:", StringComparison.Ordinal)))
            yield return $"{toolset.Name}: {toolset.GuideFile} holds a '## Skill:' procedure; a pack's guide is prose, a procedure becomes a tool";
    }

    private static IEnumerable<string> ShapeProblems(string toolset, MethodInfo method)
    {
        string tool = $"{toolset}/{method.Name}";
        if (!Regex.IsMatch(method.Name, NameRule))
            yield return $"{tool}: its name isn't {NameRule}";

        Type returns = method.ReturnType;
        bool waits = returns.IsGenericType && (returns.GetGenericTypeDefinition() == typeof(Task<>) || returns.GetGenericTypeDefinition() == typeof(ValueTask<>));
        Type text = waits ? returns.GetGenericArguments()[0] : returns;
        if (text != typeof(string))
            yield return $"{tool}: returns {TypeName(returns)}, not string, Task<string> or ValueTask<string>";

        bool hasToken = false;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken)) hasToken = true;
            else if (!IsPlainData(parameter.ParameterType, []))
                yield return $"{tool}: parameter '{parameter.Name}' is {TypeName(parameter.ParameterType)}, not plain data";
        }
        if (waits && !hasToken) yield return $"{tool}: waits but takes no CancellationToken";
    }

    private static IEnumerable<string> DescriptionProblems(string toolset, MethodInfo method)
    {
        string tool = $"{toolset}/{method.Name}";
        foreach (string identity in Enum.GetNames<IdentityKind>())
        {
            // A whole word, case ignored: "demonstrate" is not "Demo".
            var names = new Regex($@"\b{Regex.Escape(identity)}\b", RegexOptions.IgnoreCase);
            if (names.IsMatch(method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? ""))
                yield return $"{tool}: its description names the identity '{identity}'";
            foreach (ParameterInfo parameter in method.GetParameters())
                if (names.IsMatch(parameter.GetCustomAttribute<DescriptionAttribute>()?.Description ?? ""))
                    yield return $"{tool}: parameter '{parameter.Name}' names the identity '{identity}'";
        }
    }

    private static IEnumerable<string> McpProblems(string toolset, MethodInfo method)
    {
        string tool = $"{toolset}/{method.Name}";
        // The instance is made only when the tool runs, which this test never does.
        AIFunction function = AgentCatalog.FunctionOf(method, _ => throw new InvalidOperationException("the convertibility test never runs a tool"));
        McpServerTool served = McpServerTool.Create(function);
        if (served.ProtocolTool.Name != function.Name)
            yield return $"{tool}: served over MCP its name is '{served.ProtocolTool.Name}'";
        if (!JsonNode.DeepEquals(JsonNode.Parse(served.ProtocolTool.InputSchema.GetRawText()), JsonNode.Parse(function.JsonSchema.GetRawText())))
            yield return $"{tool}: served over MCP its input schema changes";
        // The SDK appends a "Returns:" line for a [return: Description]; the description must otherwise be the function's.
        if (!(served.ProtocolTool.Description ?? "").StartsWith(function.Description, StringComparison.Ordinal))
            yield return $"{tool}: served over MCP its description changes";
    }

    /// <summary>
    /// Plain data: strings, numbers, booleans, enums, dates and times, nullables and sequences of those, and our own
    /// records of those. Anything of the framework's beyond that (a client, a stream, a service) is not.
    /// </summary>
    private static bool IsPlainData(Type type, HashSet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
            || type == typeof(TimeSpan) || type == typeof(Guid))
            return true;
        if (type.IsArray) return IsPlainData(type.GetElementType()!, seen);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            return type.GetGenericArguments().All(t => IsPlainData(t, seen));
        Type? sequence = type.GetInterfaces().Prepend(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (sequence is not null) return IsPlainData(sequence.GetGenericArguments()[0], seen);
        if (type.Namespace is null || type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal)
            || type.IsInterface || type.IsAbstract || typeof(Delegate).IsAssignableFrom(type))
            return false;
        if (!seen.Add(type)) return true;
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(p => IsPlainData(p.PropertyType, seen));
    }

    private static string TypeName(Type type) =>
        type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
            : type.Name;
}

/// <summary>A pack that breaks every rule once, so the test's list of problems is checked against a known set.</summary>
internal sealed class FakeUnconvertiblePack : IAgentPack
{
    public string Name => "fakeunconvertible";

    public IReadOnlyList<ToolsetDefinition> Toolsets { get; } =
    [
        new("fakebad", "Breaks the tool rules", GuideFile: "toolsets/fakebad.md", MissingSettings: _ => [],
            CreateTools: _ => [new FakeUnconvertibleTools()], ToolType: typeof(FakeUnconvertibleTools)),
        new("fakenoguide", "Its guide isn't in its prompts", GuideFile: "toolsets/fakenoguide.md", MissingSettings: _ => [],
            CreateTools: _ => [new FakeFineTools()], ToolType: typeof(FakeFineTools)),
    ];

    public IFileProvider Prompts { get; } = new FakePrompts(new Dictionary<string, string>
    {
        ["toolsets/fakebad.md"] = "# Fakebad\n\nA guide with a procedure.\n\n## Skill: Greet\n\n- Execution:\n\n```bash\necho hi\n```\n",
    });

    public IReadOnlyList<string> Requires => [];
}

public sealed class FakeUnconvertibleTools
{
    [System.ComponentModel.Description("Finds what Puls asked for.")]
    public string FindForPuls([System.ComponentModel.Description("The town, when Puls gave none")] string area) => area;

    [System.ComponentModel.Description("Counts, in a number.")]
    public int Count([System.ComponentModel.Description("What to count")] string query) => query.Length;

    [System.ComponentModel.Description("Reads through a client it is handed.")]
    public string Read([System.ComponentModel.Description("The client")] HttpClient http) => http.ToString() ?? "";

    [System.ComponentModel.Description("Waits, with no way to stop it.")]
    public Task<string> Wait([System.ComponentModel.Description("What to wait for")] string query) => Task.FromResult(query);

    [System.ComponentModel.Description("A name one character too long.")]
    public string AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ() => "";
}

public sealed class FakeFineTools
{
    [System.ComponentModel.Description("Finds, as a pack's tool should.")]
    public Task<string> Find([System.ComponentModel.Description("What to find")] string query, CancellationToken cancellationToken = default) => Task.FromResult(query);
}
