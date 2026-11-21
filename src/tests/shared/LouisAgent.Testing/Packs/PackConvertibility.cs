using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using ModelContextProtocol.Server;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Testing.Packs;

/// <summary>
/// Every way a pack breaks the rule that packs stay convertible to an MCP server (ADR 0028), one line each; empty when
/// it is convertible. Shared by every demo's tests (F106.3.2), so each catalogue holds every pack it loads to it.
/// </summary>
/// <remarks>
/// It sees shapes, not behaviour: each tool's function is built as the engine builds it (from the tool type, so no
/// setting, browser or network), then wrapped by the MCP SDK's own <c>McpServerTool</c>, which must keep its name, its
/// input schema and its description; a tool returns text, takes plain data and a CancellationToken when it waits,
/// carries an MCP-safe name and names no identity; a pack's guides are prose in its own prompts. What it can't see (a
/// thrown failure, input read from the session, framing and bounds) is ADR 0028's review checklist.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public static partial class PackConvertibility
{
    /// <summary>What an MCP client accepts as a tool's name, and what every provider takes safely.</summary>
    public const string NameRule = @"^[A-Za-z0-9_-]{1,64}\z";

    /// <summary>The problems of <paramref name="pack"/>; <paramref name="identityNames"/> are the identities a tool may not name.</summary>
    public static IReadOnlyList<string> Problems(IAgentPack pack, IEnumerable<string> identityNames)
    {
        var identities = identityNames.ToList();
        var problems = new List<string>();
        foreach (PackToolset toolset in pack.Toolsets)
        {
            problems.AddRange(GuideProblems(pack, toolset));
            if (toolset.ToolTypes is null or { Count: 0 })
            {
                problems.Add($"{toolset.Name}: no tool type, so its tools can't be listed");
                continue;
            }

            foreach (MethodInfo method in AgentCatalog.ToolMethodsOf(toolset))
            {
                var shape = ShapeProblems(toolset.Name, method).ToList();
                problems.AddRange(shape);
                problems.AddRange(DescriptionProblems(toolset.Name, method, identities));
                // The SDK is asked only about a tool whose shape is right: it couldn't build a schema for the others.
                if (shape.Count == 0) problems.AddRange(McpProblems(toolset.Name, method));
            }
        }

        return problems;
    }

    private static IEnumerable<string> GuideProblems(IAgentPack pack, PackToolset toolset)
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

    private static IEnumerable<string> DescriptionProblems(string toolset, MethodInfo method, IReadOnlyList<string> identities)
    {
        string tool = $"{toolset}/{method.Name}";
        foreach (string identity in identities)
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
        // The instance is made only when the tool runs, which this check never does.
        AIFunction function = AgentCatalog.FunctionOf(method, _ => throw new InvalidOperationException("the convertibility check never runs a tool"));
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
