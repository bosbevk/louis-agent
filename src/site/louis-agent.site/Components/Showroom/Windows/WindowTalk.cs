namespace LouisAgent.Site.Components.Showroom.Windows;

/// <summary>
/// One moment of a business's recorded talk, as its window on the start shows it (F134.1): what the visitor asked,
/// one whole sentence of what the agent answered, the place on its website the agent showed, and the recording all
/// three are from.
/// </summary>
/// <param name="Question">What the visitor asked, word for word.</param>
/// <param name="Answer">One whole sentence of the agent's answer, word for word.</param>
/// <param name="Shown">The address of the place the agent showed, such as <c>/rooms/garden</c>.</param>
/// <param name="Recording">The recording they are from, as the site serves it.</param>
public sealed record WindowTalk(string Question, string Answer, string Shown, string Recording);
