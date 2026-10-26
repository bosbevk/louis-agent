using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Grading.Human;

/// <summary>
/// The question a person is asked to review (F18 step 6a), read from the judge prompt's own title line, so the judge and the person answer the same words and the question is not typed
/// twice: <c># Judge: is this a meal for the people asked for</c> is "Is this a meal for the people asked for?".
/// </summary>
public static class ReviewQuestion
{
    public static string From(string promptText)
    {
        string? title = promptText.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        if (title is null) throw new InvalidDataException(MessageText.Format(EvalErrorMessage.PromptHasNoTitle));

        string question = title[2..].Trim();
        if (question.StartsWith("Judge:", StringComparison.OrdinalIgnoreCase)) question = question["Judge:".Length..].Trim();
        question = char.ToUpperInvariant(question[0]) + question[1..];
        return question.EndsWith('?') ? question : question + "?";
    }
}
