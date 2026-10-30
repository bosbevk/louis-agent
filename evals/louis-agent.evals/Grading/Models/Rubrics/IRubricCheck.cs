namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// A model check that judges against a rubric: the generic <c>judge</c>, or an identity's own judge built on its own
/// rubric. A person's review of it asks the rubric's title as its question.
/// </summary>
/// <remarks>
/// A role interface (Fowler): the one thing a review needs to know about a judge, so a review can be made for any of
/// them without knowing which class it is.
/// </remarks>
public interface IRubricCheck : ICheck
{
    /// <summary>The rubric's text, the judge's system prompt.</summary>
    string Rubric { get; }
}
