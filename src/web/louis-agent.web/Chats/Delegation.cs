using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Web.Chats;

/// <summary>
/// An asked agent's or a minion's call as its card shows it: who it went to, its toolsets and task, and once it
/// returned, the answer and what the line under it says about the child (<c>[Answered by acme, did: …]</c> for an
/// <c>AskAgent</c>, <c>[Minion {role} on {llm} with {tool}]</c> for a minion). What the child cost isn't read here:
/// the card takes it from the child's own events (<see cref="Block.SubTurn"/>), never back out of the model's text
/// (FX27).
/// </summary>
/// <param name="Role">A minion's role; none for an ask, whose address says who it is.</param>
/// <param name="Handle">The kept minion's handle (<c>sub-1</c>), from a continue's call.</param>
/// <param name="Continued">True for a <c>ContinueMinion</c> call: a follow-up to a minion that was already
/// working.</param>
/// <param name="ResultAgent">The address the line under an ask's answer names: who actually answered.</param>
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
    /// <summary>The tool that hands one task and one tool to a minion (F30).</summary>
    public const string MinionToolName = "StartMinion";

    /// <summary>The tool that goes back to a minion.</summary>
    public const string ContinueMinionToolName = "ContinueMinion";

    /// <summary>The tool that asks another agent by its address (F127.3.1).</summary>
    public const string AskToolName = "AskAgent";

    /// <summary>Whether a tool call opens or continues a child: an asked agent (F127.3.1) or a minion (F30).</summary>
    public static bool IsDelegationTool(string? toolName) =>
        toolName is AskToolName or MinionToolName or ContinueMinionToolName;

    /// <summary>Where the line under an asked agent's answer starts (core's AgentAnswer).</summary>
    private const string AnsweredBy = "\n\n[Answered by ";

    /// <summary>The delegation in a block, or none when the block isn't an ask or a minion's call.</summary>
    public static Delegation? From(Block block)
    {
        if (block.Kind != BlockKind.Tool || !IsDelegationTool(block.ToolName)) return null;
        return block.ToolName is MinionToolName or ContinueMinionToolName ? FromMinion(block) : FromAsk(block);
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

    /// <summary>
    /// An ask (F127.3.1): the address and the message from the call; once it returned, the answer without the line
    /// under it, and who that line says answered. What the child loaded comes from its own stream.
    /// </summary>
    private static Delegation FromAsk(Block block)
    {
        string address = "", task = "";
        try
        {
            using var input = JsonDocument.Parse(block.ToolInput ?? "{}");
            var root = input.RootElement;
            if (root.TryGetProperty("address", out var a) && a.ValueKind == JsonValueKind.String)
                address = a.GetString() ?? "";
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                task = m.GetString() ?? "";
        }
        catch (JsonException)
        {
            // Unreadable input: the card shows what it can.
        }

        string? answer = null, resultAgent = null;
        bool failed = block.ToolResult?.StartsWith("Error:", StringComparison.Ordinal) == true;
        int line = block.ToolResult?.LastIndexOf(AnsweredBy, StringComparison.Ordinal) ?? -1;
        if (!failed && line >= 0)
        {
            answer = block.ToolResult![..line].TrimEnd();
            // "seemeeu-guesthouse, did: SendLead]": the address is what comes before the comma.
            string named = block.ToolResult[(line + AnsweredBy.Length)..];
            resultAgent = Blank(named[..Math.Max(named.IndexOf(','), 0)]);
        }

        // No role: the address already says who it is (acme, acme.cook), where a voice's role is its mode's path.
        IReadOnlyList<string> toolsets = block.SubTurn?.Toolsets ?? [];
        return new Delegation(address, toolsets, task, answer ?? (failed ? block.ToolResult : null), failed,
            resultAgent);
    }

    /// <summary>The text, trimmed, or none when it's empty.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>A minion's footer at the end of its answer: <c>[Minion {role} on {llm} with {tool}]</c>.</summary>
    [GeneratedRegex(@"\n\n\[Minion (?<role>\S+) on (?<llm>\S+) with (?<tool>\S+)\]\s*$")]
    private static partial Regex MinionFooter();
}
