using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models.Rubrics.Sources;

/// <summary>
/// What the tools a judge is told about gave back in the trial: each result under its tool's name, in the order the
/// calls were made, a specialist's calls too. A result is cut at <see cref="ResultLimit"/> characters and the cut is
/// said in the request, and a tool that was never called gets one line saying so.
/// </summary>
/// <remarks>
/// The same shape as <see cref="JudgeAsk"/>, an evaluation context the judge's evaluator reads, so a judge that is
/// told nothing about tools is sent the request it always was. The limit is for one result: a recipe print is at
/// most 60 ingredients and 60 steps or 8,000 characters of a page's text, so 12,000 holds a whole print and still
/// bounds a result that is not one (ADR 0005). It is not a count of results: a trial's tools are bounded by the
/// agent's own prompt.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0005-guard-the-context.md">ADR 0005: guard the context</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public sealed class JudgeSources(string lines) : EvaluationContext("sources", lines)
{
    /// <summary>How many characters of one tool result the judge is sent; what is past it is cut, and said.</summary>
    public const int ResultLimit = 12_000;

    /// <summary>The request's text for these tools, empty when the judge is told no tool.</summary>
    public string Lines { get; } = lines;

    /// <summary>
    /// The heading, then each tool's results in the order the setting lists the tools, every part ending in a blank
    /// line so the answer follows; nothing at all when no tool is listed.
    /// </summary>
    public static JudgeSources Of(Trial trial, IReadOnlyList<string> tools)
    {
        if (tools.Count == 0) return new JudgeSources("");

        var parts = new List<string> { MessageText.Format(JudgeRequestMessage.SourcesHeading) };
        parts.AddRange(tools.SelectMany(tool => Results(trial, tool)));
        return new JudgeSources(string.Join("\n\n", parts) + "\n\n");
    }

    /// <summary>
    /// One part for each result of the tool in the trial, in order, or the one line saying it gave none.
    /// </summary>
    private static IEnumerable<string> Results(Trial trial, string tool)
    {
        var results = trial.Transcript.Turns.SelectMany(turn => EngineEvents.ResultsOfTool(turn, tool)).ToList();
        if (results.Count == 0) return [MessageText.Format(JudgeRequestMessage.SourceNone, new { Tool = tool })];

        return results.Select((result, index) => MessageText.Format(JudgeRequestMessage.SourceResult, new
        {
            Tool = tool,
            Number = index + 1,
            results.Count
        }) + "\n" + Limited(result));
    }

    /// <summary>
    /// The result, or its first <see cref="ResultLimit"/> characters and a line saying how much was cut.
    /// </summary>
    private static string Limited(string result) =>
        result.Length <= ResultLimit
            ? result
            : result[..ResultLimit] + "\n" + MessageText.Format(JudgeRequestMessage.SourceCut,
                new
                {
                    Shown = ResultLimit,
                    Total = result.Length
                });
}
