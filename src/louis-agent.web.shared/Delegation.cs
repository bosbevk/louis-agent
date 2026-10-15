using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Web.Shared;

/// <summary>
/// A <c>StartSubSession</c> or <c>ContinueSubSession</c> call read from its block (F5, F26): the specialist, its role,
/// the toolsets, the task, and, once it returned, the answer and the footer the tool appends
/// (<c>[Sub-session on coder (files): 3 requests, …]</c>, or for a kept one
/// <c>[Sub-session sub-1 on puls (web; puls-cook): 3 requests, …. It stays open: …]</c>).
/// </summary>
/// <param name="Role">The specialist's role (its personality): from the call for a start, from the footer once it returned.</param>
/// <param name="Handle">The kept sub-session's handle (<c>sub-1</c>): from the call for a continue, from the footer for a start.</param>
/// <param name="Continued">True for a <c>ContinueSubSession</c> call: a follow-up to a specialist that was already working.</param>
/// <param name="ResultAgent">The peer named in the result's footer: the one actually used (the call may leave the name empty).</param>
public sealed partial record Delegation(
    string Agent, IReadOnlyList<string> Toolsets, string? Task, string? Answer, string? UsageLine, bool Failed, string? ResultAgent = null,
    string? Role = null, string? Handle = null, bool Continued = false)
{
    public const string ToolName = "StartSubSession";
    public const string ContinueToolName = "ContinueSubSession";

    /// <summary>The delegation in <paramref name="block"/>, or null when it isn't a start or continue call.</summary>
    public static Delegation? From(Block block)
    {
        if (block.Kind != BlockKind.Tool || block.ToolName is not (ToolName or ContinueToolName)) return null;
        bool continued = block.ToolName == ContinueToolName;

        string agent = "", task = "";
        string? role = null, handle = null;
        var toolsets = new List<string>();
        try
        {
            using var input = JsonDocument.Parse(block.ToolInput ?? "{}");
            var root = input.RootElement;
            if (root.TryGetProperty("agent", out var a)) agent = a.GetString() ?? "";
            if (root.TryGetProperty("message", out var m)) task = m.GetString() ?? "";
            if (root.TryGetProperty("personality", out var p) && p.ValueKind == JsonValueKind.String) role = Blank(p.GetString());
            if (root.TryGetProperty("handle", out var h) && h.ValueKind == JsonValueKind.String) handle = Blank(h.GetString());
            if (root.TryGetProperty("toolsets", out var t) && t.ValueKind == JsonValueKind.Array)
                toolsets.AddRange(t.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0));
        }
        catch (JsonException)
        {
            // Unreadable input: the card shows what it can.
        }

        string? answer = null, usage = null, resultAgent = null;
        bool failed = block.ToolResult?.StartsWith("Error:", StringComparison.Ordinal) == true;
        if (block.ToolResult is { } result && !failed && Footer().Match(result) is { Success: true } match)
        {
            answer = result[..match.Index].TrimEnd();
            usage = match.Groups["usage"].Value;
            resultAgent = match.Groups["agent"].Value;
            handle ??= Blank(match.Groups["handle"].Value);
            // The footer's "(web; puls-cook)": the toolsets, then the role; the old footer has only the toolsets.
            string[] inside = match.Groups["inside"].Value.Split(';', 2, StringSplitOptions.TrimEntries);
            if (inside.Length == 2) role ??= Blank(inside[1]);
            if (toolsets.Count == 0)
                toolsets.AddRange(inside[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }
        return new Delegation(agent, toolsets, task, answer ?? (failed ? block.ToolResult : null), usage, failed, resultAgent, role, handle, continued);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"\n\n\[Sub-session (?:(?<handle>\S+) )?on (?<agent>\S+) \((?<inside>[^)]*)\): (?<usage>[^\]]*?)(?:\. It stays open:[^\]]*)?\]\s*$")]
    private static partial Regex Footer();
}
