namespace LouisAgent.Web.Admin.Studio.Compose;

/// <summary>One rubric of an eval being composed: its file's name in <c>Rubrics/</c> and its text, as typed.</summary>
/// <remarks>A class: the form's inputs bind to it and change it as the person types (ADR 0040).</remarks>
public sealed class RubricEntry
{
    /// <summary>The file's name, such as <c>meal-for-people.md</c>.</summary>
    public string Name { get; set; } = "";

    /// <summary>The rubric's text.</summary>
    public string Text { get; set; } = "";
}
