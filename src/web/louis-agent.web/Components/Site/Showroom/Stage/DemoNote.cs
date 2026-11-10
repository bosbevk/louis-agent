namespace LouisAgent.Web.Components.Site.Showroom.Stage;

/// <summary>
/// One thing a custom agent could add for a business like the demo's (F127.1): its title and a line of what it does.
/// </summary>
/// <param name="Title">What it adds, in a few words: "Book into your booking system".</param>
/// <param name="Text">A line of how.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record DemoNote(string Title, string Text);
