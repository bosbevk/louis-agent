using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Judging;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// Marks one criterion with one model call. It sends the conversation up to and including the turn it marks, every
/// tool call with its result as the agent got it, and the one criterion, and asks for the answer as a typed record:
/// the reason first, then met or not.
/// </summary>
/// <remarks>
/// One call per criterion (Louis's pick, 2026-11-22), chosen over one call per conversation: an isolated judge for
/// each dimension, as Anthropic's evals guidance asks. It sets no temperature and forces no tool; the record's schema
/// goes on the request through <c>GetResponseAsync&lt;T&gt;</c>. The call runs in the ledger's <c>grading</c> scope
/// (ADR 0007). A judge that can't mark throws <see cref="JudgeFailedException"/>, never "not met", since that would
/// blame the agent for the judge's trouble.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class Judge(IChatClient model)
{
    /// <summary>The criterion's mark for the conversation's last turn.</summary>
    public async Task<Mark> MarkAsync(
        string criterion, IReadOnlyList<TurnTranscript> conversation, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> request =
        [
            new(ChatRole.System, MessageText.Format(JudgeMessage.Instructions)),
            new(ChatRole.User, Laid(criterion, conversation))
        ];

        // The call is priced under grading, whichever scope the run that asked is in.
        using var grading = UsageScope.Begin(UsagePurpose.Grading);
        ChatResponse<JudgeAnswer> response;
        try
        {
            response = await model.GetResponseAsync<JudgeAnswer>(request, cancellationToken: cancellationToken);
        }
        catch (Exception failed) when (failed is not OperationCanceledException)
        {
            var modelFailed = new ModelFailedHoles(failed.Message);
            throw new JudgeFailedException(MessageText.Format(JudgeMessage.ModelFailed, modelFailed), failed);
        }

        return MarkOf(criterion, response);
    }

    /// <summary>The conversation as the judge reads it: each turn under its number, then the one criterion.</summary>
    private static string Laid(string criterion, IReadOnlyList<TurnTranscript> conversation)
    {
        var parts = conversation.SelectMany((turn, at) => Laid(turn, at + 1)).ToList();
        var marked = new CriterionHoles(criterion);
        parts.Add(MessageText.Format(JudgeMessage.TheCriterion, marked));
        return string.Join("\n\n", parts);
    }

    /// <summary>One turn: its number, what the person asked, each tool call, and the agent's answer.</summary>
    private static IEnumerable<string> Laid(TurnTranscript turn, int number)
    {
        var heading = new TurnNumberHoles(number);
        yield return MessageText.Format(JudgeMessage.TurnHeading, heading);

        var asked = new AskedHoles(turn.Ask);
        yield return MessageText.Format(JudgeMessage.PersonAsked, asked);

        foreach (var call in turn.Calls)
        {
            var called = new ToolCalledHoles(call.Tool, call.Input, call.Result);
            yield return MessageText.Format(call.Failed ? JudgeMessage.ToolFailed : JudgeMessage.ToolCalled, called);
        }

        var answered = new AnsweredHoles(turn.Answer);
        yield return MessageText.Format(JudgeMessage.AgentAnswered, answered);
    }

    /// <summary>The model's answer as a mark, or why it can't be one.</summary>
    private static Mark MarkOf(string criterion, ChatResponse<JudgeAnswer> response)
    {
        if (string.IsNullOrWhiteSpace(response.Text))
            throw new JudgeFailedException(MessageText.Format(JudgeMessage.NoAnswer));

        var answer = Read(response);
        var unfit = new AnsweredHoles(response.Text);
        if (answer is null || string.IsNullOrWhiteSpace(answer.Reason))
            throw new JudgeFailedException(MessageText.Format(JudgeMessage.AnswerDoesntFit, unfit));

        return new Mark(criterion, answer.Met, answer.Reason);
    }

    /// <summary>The typed answer, or none when the model's text isn't the record.</summary>
    private static JudgeAnswer? Read(ChatResponse<JudgeAnswer> response)
    {
        try
        {
            return response.TryGetResult(out var answer) ? answer : null;
        }
        catch (JsonException)
        {
            // Text that isn't the record's JSON is an answer that doesn't fit, said as such by the caller.
            return null;
        }
    }
}
