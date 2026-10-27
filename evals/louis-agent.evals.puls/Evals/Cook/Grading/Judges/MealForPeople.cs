using LouisAgent.Core.Messages;
using LouisAgent.Evals.Puls.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>
/// The cook's judge (F18 step 6.2): an <see cref="IEvaluator"/> with one yes-or-no metric, "is this answer a meal for the people asked for". It sends the judge model its prompt as
/// the system message, and as the user message the ask (<see cref="CookAsk"/>: people, town), the earlier turns when there are any (6a.4f) and the answer; it reads a <see cref="MealVerdict"/> from the reply. A reply it
/// can't read is a metric with no value and the reason why, never a pass: the grader above maps no value to Unknown.
/// </summary>
public sealed class MealForPeople(string prompt) : IEvaluator
{
    public const string MetricName = "meal-for-people";

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public async ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        var ask = additionalContext?.OfType<CookAsk>().FirstOrDefault() ?? new CookAsk(null, null);
        var request = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User,
                $"The ask:\n{ask.Contents.OfType<TextContent>().Single().Text}\n{SoFar(messages)}\nThe answer to judge:\n{modelResponse.Text}"),
        };

        // Temperature 0: the same answer should get the same verdict, or the calibration measures the dice.
        var reply = await chatConfiguration!.ChatClient.GetResponseAsync(request, new ChatOptions { Temperature = 0 },
            cancellationToken);

        var verdict = MealVerdict.Read(reply.Text);
        string reason = verdict is null
            ? MessageText.Format(GradeMessage.JudgeReplyUnreadable, new { Reply = Clip(reply.Text) })
            : verdict.Reason ?? "";
        return new EvaluationResult(new BooleanMetric(MetricName, verdict?.Meal, reason));
    }

    /// <summary>The earlier turns, question and answer, when there are any (6a.4f): a follow-up's answer relies on the recipe given before it. A single turn adds nothing.</summary>
    private static string SoFar(IEnumerable<ChatMessage> messages)
    {
        var turns = messages.Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant).ToList();
        if (!turns.Any(m => m.Role == ChatRole.Assistant)) return "";
        return "\nThe conversation so far:\n" + string.Join("\n",
            turns.Select(m => $"{(m.Role == ChatRole.User ? "User" : "Assistant")}: {m.Text}")) + "\n";
    }

    private static string Clip(string text) => text.Length <= 120 ? text.Trim() : text[..120].Trim() + "…";
}
