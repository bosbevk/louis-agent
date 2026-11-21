namespace LouisAgent.Web.Components.Site.Text;

/// <summary>One step of a few: its short title, and what happens in it, in Markdown.</summary>
/// <param name="Title">The step in a few words, such as "A short call."</param>
/// <param name="Text">What happens in it, in Markdown, so it may hold a link or a name in code.</param>
public sealed record Step(string Title, string Text);
