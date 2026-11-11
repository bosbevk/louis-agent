using System.Text.Json;
using LouisAgent.Core.Delegation.Agents;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Delegation;

/// <summary>
/// Finds the report to show when a turn ended on a specialist's answer, so the user gets what the specialist wrote and
/// the model doesn't write it out again. The footer the tool adds is for the model, so it's left off.
/// </summary>
internal static class ShownReport
{
    /// <summary>Where the tool's footer starts, under the specialist's answer.</summary>
    private const string Footer = "\n\n[Sub-session ";

    /// <summary>Where the line under an asked agent's answer starts (F127.3.1).</summary>
    private const string AnsweredBy = "\n\n[Answered by ";

    private static readonly string[] Tools =
    [
        nameof(SpecialistTools.StartSubSession), nameof(SpecialistTools.ContinueSubSession),
        nameof(AskAgentTools.AskAgent)
    ];

    /// <summary>
    /// The report when the last thing in the turn is a specialist's answer, with the text set apart from what came
    /// before it; null when the model had the last word, as it does on every other turn.
    /// </summary>
    public static string? In(IReadOnlyList<AIContent> contents)
    {
        // The tool's results, from the end: usage may follow them, and anything else means the model answered.
        var results = contents.Reverse().Where(content => content is not UsageContent)
            .TakeWhile(content => content is FunctionResultContent).OfType<FunctionResultContent>().Reverse().ToList();
        if (results.Count == 0) return null;

        var names = contents.OfType<FunctionCallContent>().ToDictionary(call => call.CallId, call => call.Name);
        if (!results.All(result => names.TryGetValue(result.CallId, out string? name) && Tools.Contains(name)))
            return null;

        return "\n\n" + string.Join("\n\n", results.Select(result => WithoutFooter(TextOf(result.Result))));
    }

    /// <summary>The text of a tool's result, however the tool loop holds it.</summary>
    private static string TextOf(object? result) =>
        result is JsonElement { ValueKind: JsonValueKind.String } text
            ? text.GetString() ?? ""
            : result?.ToString() ?? "";

    /// <summary>The answer alone: the footer holds the handle, which is the model's.</summary>
    private static string WithoutFooter(string answer)
    {
        int footer = Math.Max(answer.LastIndexOf(Footer, StringComparison.Ordinal),
            answer.LastIndexOf(AnsweredBy, StringComparison.Ordinal));
        return footer < 0 ? answer : answer[..footer];
    }
}
