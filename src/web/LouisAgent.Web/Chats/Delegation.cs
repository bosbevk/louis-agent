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
/// <param name="Role">A minion's address; none for an ask, whose address says who it is.</param>
/// <param name="ResultAgent">The address the line under an ask's answer names: who actually answered.</param>
/// <param name="Minion">True for an ask that hands a tool (F30, F127.5): an agent handing one page and one of its
/// tools to a minion. <paramref name="Toolsets"/> then holds that one tool, <paramref name="Role"/> the minion's
/// address and <paramref name="Llm"/> the model it ran on.</param>
public sealed partial record Delegation(
    string Agent,
    IReadOnlyList<string> Toolsets,
    string? Task,
    string? Answer,
    bool Failed,
    string? ResultAgent = null,
    string? Role = null,
    bool Minion = false,
    string? Llm = null)
{
    /// <summary>The tool that asks another agent, or a minion, by its address (F127.3.1, F127.5).</summary>
    public const string AskToolName = "AskAgent";

    /// <summary>Whether a tool call opens or continues a child: an asked agent or a minion, both by AskAgent.</summary>
    public static bool IsDelegationTool(string? toolName) => toolName is AskToolName;

    /// <summary>Where the line under an asked agent's answer starts (core's AgentAnswer).</summary>
    private const string AnsweredBy = "\n\n[Answered by ";

    /// <summary>
    /// The delegation in a block, or none when the block isn't an ask: a minion's card when the ask hands a tool,
    /// since only a minion takes one.
    /// </summary>
    public static Delegation? From(Block block)
    {
        if (block.Kind != BlockKind.Tool || !IsDelegationTool(block.ToolName)) return null;
        return HandsATool(block) ? FromMinion(block) : FromAsk(block);
    }

    /// <summary>Whether the ask's input names a tool, which makes it a minion's.</summary>
    private static bool HandsATool(Block block)
    {
        try
        {
            using var input = JsonDocument.Parse(block.ToolInput ?? "{}");
            return input.RootElement.TryGetProperty("tool", out var tool) && tool.ValueKind == JsonValueKind.String &&
                   Blank(tool.GetString()) is not null;
        }
        catch (JsonException)
        {
            // Unreadable input: an ordinary ask's card shows what it can.
            return false;
        }
    }

    /// <summary>
    /// A minion's ask (F30, F127.5): its address, the one tool and the task, and once it returned, its answer and
    /// the model the footer names.
    /// </summary>
    private static Delegation FromMinion(Block block)
    {
        string task = "";
        string? role = null, tool = null;
        using (var input = JsonDocument.Parse(block.ToolInput ?? "{}"))
        {
            var root = input.RootElement;
            // A minion is asked by its address; the footer's role file is the fallback.
            if (root.TryGetProperty("address", out var r) && r.ValueKind == JsonValueKind.String)
                role = Blank(r.GetString());
            if (root.TryGetProperty("tool", out var t) && t.ValueKind == JsonValueKind.String)
                tool = Blank(t.GetString());
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                task = m.GetString() ?? "";
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
            failed, null, role, Minion: true, Llm: llm);
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
