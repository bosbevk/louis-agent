namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>One thing the agent does, as a card shows it: its film, its title, its words and what it called.</summary>
/// <param name="Film">The small film the card plays.</param>
/// <param name="Title">What it does, in a few words.</param>
/// <param name="Text">How, in a sentence or two, in Markdown.</param>
/// <param name="Call">The tool or the part that does it, shown as a pill.</param>
public sealed record Feature(FeatureFilm Film, string Title, string Text, string Call);
