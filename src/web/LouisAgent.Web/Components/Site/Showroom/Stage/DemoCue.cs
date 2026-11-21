namespace LouisAgent.Web.Components.Site.Showroom.Stage;

/// <summary>
/// One thing to watch for in a demo (F127.1): the turn of the recording it is about, counted from one, and what to
/// watch, in about 25 words at most (the research: the best demos' captions run 25 to 30 words).
/// </summary>
public sealed record DemoCue(int Turn, string Text);
