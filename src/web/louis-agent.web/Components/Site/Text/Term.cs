namespace LouisAgent.Web.Components.Site.Text;

/// <summary>One entry of a term list: the names it is about, shown as code, and a sentence on them in Markdown.</summary>
/// <param name="Names">The names, such as projects or folders; one or more, shown side by side.</param>
/// <param name="Text">What they are, in Markdown, so a name inside it can be code too.</param>
public sealed record Term(IReadOnlyList<string> Names, string Text);
