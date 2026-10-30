using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// The evaluator behind a rubric's judge: it sends the judge model the rubric as the system message and, as the user
/// message, what it was told about the scenario, the earlier turns when there are any and the answer; then it reads
/// one <see cref="JudgeVerdict"/> from the reply as a yes-or-no metric.
/// </summary>
/// <remarks>
/// An <see cref="IEvaluator"/> from Microsoft.Extensions.AI.Evaluation, so <see cref="EvaluatorCheck"/> maps its
/// metric to a grade as for any model check: a reply it can't read is a metric with no value and the reason why,
/// which the check makes Unknown, never a pass.
/// </remarks>
public sealed class RubricJudge(string metric, string rubric) : IEvaluator
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> EvaluationMetricNames => [metric];

    /// <summary>
    /// Asks the judge, at temperature 0 so the same answer gets the same verdict, and reads its reply.
    /// </summary>
    public async ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        var ask = additionalContext?.OfType<JudgeAsk>().FirstOrDefault()?.Lines ?? "";
        var request = new List<ChatMessage>
        {
            new(ChatRole.System, rubric),
            new(ChatRole.User, Told(ask) + SoFar(messages) + $"The answer to judge:\n{modelResponse.Text}")
        };
        var reply = await chatConfiguration!.ChatClient.GetResponseAsync(request, new ChatOptions { Temperature = 0 },
            cancellationToken);

        var verdict = JudgeVerdict.Read(reply.Text);
        var reason = verdict is null
            ? MessageText.Format(CommonGradeMessage.JudgeReplyUnreadable, new { Reply = Clip(reply.Text) })
            : verdict.Reason ?? "";
        return new EvaluationResult(new BooleanMetric(metric, verdict?.Pass, reason));
    }

    /// <summary>What the judge is told about the scenario, when it is told anything.</summary>
    private static string Told(string ask) => ask.Length == 0 ? "" : $"The ask:\n{ask}\n\n";

    /// <summary>
    /// The earlier turns, question and answer, when there are any: a follow-up's answer relies on what came before.
    /// A single turn adds nothing.
    /// </summary>
    private static string SoFar(IEnumerable<ChatMessage> messages)
    {
        var turns = messages.Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant).ToList();
        if (turns.All(m => m.Role != ChatRole.Assistant)) return "";
        return "The conversation so far:\n" + string.Join("\n",
            turns.Select(m => $"{(m.Role == ChatRole.User ? "User" : "Assistant")}: {m.Text}")) + "\n\n";
    }

    /// <summary>The start of a reply, for a reason that quotes it.</summary>
    private static string Clip(string text) => text.Length <= 120 ? text.Trim() : text[..120].Trim() + "…";
}
