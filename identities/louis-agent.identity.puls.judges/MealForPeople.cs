using LouisAgent.Core.Messages;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Identities.Puls.Judges;

/// <summary>
/// Asks the judge model one yes-or-no question: is this answer a meal for the people asked for? It sends the rubric
/// as the system message, then the ask (<see cref="CookAsk"/>), the earlier turns if there are any, and the answer, and
/// reads a <see cref="MealVerdict"/> back.
/// </summary>
/// <remarks>
/// Adapter: it fits Microsoft.Extensions.AI.Evaluation's <see cref="IEvaluator"/>, so the harness's judge runs it like
/// any other evaluator. A reply it can't read gives no value and says why, never a pass; the check above it turns no
/// value into Unknown.
/// </remarks>
public sealed class MealForPeople(string prompt) : IEvaluator
{
    /// <summary>The metric's name, which is also the check's.</summary>
    public const string MetricName = "meal-for-people";

    /// <summary>The one metric it gives.</summary>
    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    /// <summary>Asks the judge about the answer and turns its reply into the metric.</summary>
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

    /// <summary>
    /// The earlier turns, question and answer, if there are any: a follow-up's answer relies on the recipe given before
    /// it. A single turn adds nothing.
    /// </summary>
    private static string SoFar(IEnumerable<ChatMessage> messages)
    {
        var turns = messages.Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant).ToList();
        if (!turns.Any(m => m.Role == ChatRole.Assistant)) return "";
        return "\nThe conversation so far:\n" + string.Join("\n",
            turns.Select(m => $"{(m.Role == ChatRole.User ? "User" : "Assistant")}: {m.Text}")) + "\n";
    }

    /// <summary>The start of a reply, short enough to quote in a reason.</summary>
    private static string Clip(string text) => text.Length <= 120 ? text.Trim() : text[..120].Trim() + "…";
}
