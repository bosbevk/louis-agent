using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Web.Shared.Chats;

/// <summary>
/// A specialist or minion call as its card shows it: who it went to, its role, toolsets and task, and once it
/// returned, the answer and what the footer says about the child (<c>[Sub-session sub-1 on puls (web; puls-cook). It
/// stays open: …]</c>, or <c>[Minion {role} on {llm} with {tool}]</c>). What the child cost isn't read here: the card
/// takes it from the child's own events (<see cref="Block.SubTurn"/>), never back out of the model's text (FX27).
/// </summary>
/// <param name="Role">The specialist's role: from the call for a start, from the footer once it returned.</param>
/// <param name="Handle">
/// The kept sub-session's handle (<c>sub-1</c>): from the call for a continue, from the footer for a start.
/// </param>
/// <param name="Continued">
/// True for a <c>ContinueSubSession</c> call: a follow-up to a specialist that was already working.
/// </param>
/// <param name="ResultAgent">
/// The peer named in the result's footer: the one actually used (the call may leave the name empty).
/// </param>
/// <param name="Minion">True for a <c>StartMinion</c> or <c>ContinueMinion</c> call (F30): a specialist handing one
/// page and one of its tools to a minion. <paramref name="Toolsets"/> then holds that one tool,
/// <paramref name="Role"/> the minion's role and <paramref name="Llm"/> the model it ran on.</param>
public sealed partial record Delegation(
    string Agent,
    IReadOnlyList<string> Toolsets,
    string? Task,
    string? Answer,
    bool Failed,
    string? ResultAgent = null,
    string? Role = null,
    string? Handle = null,
    bool Continued = false,
    bool Minion = false,
    string? Llm = null)
{
    public const string ToolName = "StartSubSession";
    public const string ContinueToolName = "ContinueSubSession";
    public const string MinionToolName = "StartMinion";
    public const string ContinueMinionToolName = "ContinueMinion";

    /// <summary>Whether a tool call opens or continues a child: a specialist (F5, F26) or a minion (F30).</summary>
    public static bool IsDelegationTool(string? toolName) =>
        toolName is ToolName or ContinueToolName or MinionToolName or ContinueMinionToolName;

    /// <summary>The delegation in a block, or none when the block isn't a start or continue call.</summary>
    public static Delegation? From(Block block)
    {
        if (block.Kind != BlockKind.Tool || !IsDelegationTool(block.ToolName)) return null;
        if (block.ToolName is MinionToolName or ContinueMinionToolName) return FromMinion(block);
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
            // CL14: the call's role; a chat recorded before CL14 said personality.
            if ((root.TryGetProperty("role", out var p) || root.TryGetProperty("personality", out p)) &&
                p.ValueKind == JsonValueKind.String)
                role = Blank(p.GetString());
            if (root.TryGetProperty("handle", out var h) && h.ValueKind == JsonValueKind.String)
                handle = Blank(h.GetString());
            if (root.TryGetProperty("toolsets", out var t) && t.ValueKind == JsonValueKind.Array)
                toolsets.AddRange(t.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0));
        }
        catch (JsonException)
        {
            // Unreadable input: the card shows what it can.
        }

        string? answer = null, resultAgent = null;
        bool failed = block.ToolResult?.StartsWith("Error:", StringComparison.Ordinal) == true;
        if (block.ToolResult is { } result && !failed && Footer().Match(result) is { Success: true } match)
        {
            answer = result[..match.Index].TrimEnd();
            resultAgent = match.Groups["agent"].Value;
            handle ??= Blank(match.Groups["handle"].Value);
            // The footer's "(web; puls-cook)": the toolsets, then the role; a start that isn't kept has no role there.
            string[] inside = match.Groups["inside"].Value.Split(';', 2, StringSplitOptions.TrimEntries);
            if (inside.Length == 2) role ??= Blank(inside[1]);
            // F34: the footer names what the specialist loaded (its role may add some), so it wins over what was sent.
            var loaded = inside[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (loaded.Length > 0) toolsets = [.. loaded];
        }

        // While it works, its own stream's start says what it loaded (F34).
        if (block.SubTurn?.Toolsets is { Count: > 0 } live) toolsets = [.. live];
        // FX17: a follow-up's call names the handle, not the role; its stream's start names it.
        role ??= Blank(block.SubTurn?.Role);
        return new Delegation(agent, toolsets, task, answer ?? (failed ? block.ToolResult : null), failed, resultAgent,
            role, handle, continued);
    }

    /// <summary>
    /// A minion call (F30): its role, the one tool and the task, and once it returned, its answer and the model the
    /// footer names.
    /// </summary>
    private static Delegation FromMinion(Block block)
    {
        bool continued = block.ToolName == ContinueMinionToolName;
        string task = "";
        string? role = null, tool = null, handle = null;
        try
        {
            using var input = JsonDocument.Parse(block.ToolInput ?? "{}");
            var root = input.RootElement;
            if (root.TryGetProperty("role", out var r) && r.ValueKind == JsonValueKind.String)
                role = Blank(r.GetString());
            if (root.TryGetProperty("tool", out var t) && t.ValueKind == JsonValueKind.String)
                tool = Blank(t.GetString());
            if (root.TryGetProperty("task", out var k)) task = k.GetString() ?? "";
            else if (root.TryGetProperty("message", out var m)) task = m.GetString() ?? "";
            if (root.TryGetProperty("handle", out var h) && h.ValueKind == JsonValueKind.String)
                handle = Blank(h.GetString());
        }
        catch (JsonException)
        {
            // Unreadable input: the card shows what it can.
        }

        string? answer = null, llm = null;
        bool failed = block.ToolResult?.StartsWith("Error:", StringComparison.Ordinal) == true;
        if (block.ToolResult is { } result && !failed && MinionFooter().Match(result) is { Success: true } match)
        {
            answer = result[..match.Index].TrimEnd();
            llm = Blank(match.Groups["llm"].Value);
            role ??= Blank(match.Groups["role"].Value);
            tool ??= Blank(match.Groups["tool"].Value);
        }

        return new Delegation("", tool is null ? [] : [tool], task, answer ?? (failed ? block.ToolResult : null),
            failed, null, role, handle, continued, Minion: true, Llm: llm);
    }

    /// <summary>The text, trimmed, or none when it's empty.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// A specialist's footer at the end of its answer: <c>[Sub-session on coder (files)]</c>, or for a kept one
    /// <c>[Sub-session sub-1 on puls (web; puls-cook). It stays open: …]</c>.
    /// </summary>
    [GeneratedRegex(
        @"\n\n\[Sub-session (?:(?<handle>\S+) )?on (?<agent>\S+) \((?<inside>[^)]*)\)" +
        @"(?:\. It stays open:[^\]]*)?\]\s*$")]
    private static partial Regex Footer();

    /// <summary>A minion's footer at the end of its answer: <c>[Minion {role} on {llm} with {tool}]</c>.</summary>
    [GeneratedRegex(@"\n\n\[Minion (?<role>\S+) on (?<llm>\S+) with (?<tool>\S+)\]\s*$")]
    private static partial Regex MinionFooter();
}
