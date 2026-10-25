using System.ComponentModel;

namespace LouisAgent.Evals.Template.Messages;

/// <summary>This set's texts (ADR 0009: a text is a message enum member). A set keeps its eval titles, grader reasons and refusals here.</summary>
public enum TemplateMessage
{
    [Description("What this eval proves, in one sentence: the portal shows it as the eval's title.")]
    ExampleProves,
}
