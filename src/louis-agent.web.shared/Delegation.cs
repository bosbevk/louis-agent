using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Web.Shared;

/// <summary>
/// A <c>StartSubSession</c> call read from its block (F5): the specialist, the toolsets, the task, and, once it returned,
/// the answer and the usage line the tool appends (<c>[Sub-session on coder (files): 3 requests, …]</c>).
/// </summary>
/// <param name="ResultAgent">The peer named in the result's usage line: the one actually used (the call may leave the name empty).</param>
public sealed partial record Delegation(
    string Agent, IReadOnlyList<string> Toolsets, string? Task, string? Answer, string? UsageLine, bool Failed, string? ResultAgent = null)
{
    public const string ToolName = "StartSubSession";

    /// <summary>The delegation in <paramref name="block"/>, or null when it isn't a StartSubSession call.</summary>
    public static Delegation? From(Block block)
    {
        if (block.Kind != BlockKind.Tool || block.ToolName != ToolName) return null;

        string agent = "", task = "";
        var toolsets = new List<string>();
        try
        {
            using var input = JsonDocument.Parse(block.ToolInput ?? "{}");
            var root = input.RootElement;
            if (root.TryGetProperty("agent", out var a)) agent = a.GetString() ?? "";
            if (root.TryGetProperty("message", out var m)) task = m.GetString() ?? "";
            if (root.TryGetProperty("toolsets", out var t) && t.ValueKind == JsonValueKind.Array)
                toolsets.AddRange(t.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0));
        }
        catch (JsonException)
        {
            // Unreadable input: the card shows what it can.
        }

        string? answer = null, usage = null, resultAgent = null;
        bool failed = block.ToolResult?.StartsWith("Error:", StringComparison.Ordinal) == true;
        if (block.ToolResult is { } result && !failed && UsageSuffix().Match(result) is { Success: true } match)
        {
            answer = result[..match.Index].TrimEnd();
            usage = match.Groups["usage"].Value;
            resultAgent = match.Groups["agent"].Value;
        }
        return new Delegation(agent, toolsets, task, answer ?? (failed ? block.ToolResult : null), usage, failed, resultAgent);
    }

    [GeneratedRegex(@"\n\n\[Sub-session on (?<agent>\S+) [^\]]*?\): (?<usage>[^\]]*)\]\s*$")]
    private static partial Regex UsageSuffix();
}
