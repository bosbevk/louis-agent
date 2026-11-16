namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>One photograph in a fan: where it leads, its picture, and its caption's title and line.</summary>
/// <param name="Title">The caption's title, such as "A guesthouse".</param>
/// <param name="Line">The caption's one line under it.</param>
/// <param name="Image">The photograph's address.</param>
/// <param name="Href">Where pressing it goes.</param>
public sealed record Shot(string Title, string Line, string Image, string Href);
