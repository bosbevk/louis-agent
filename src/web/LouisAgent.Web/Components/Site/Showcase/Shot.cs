namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>
/// One photograph in a fan: where it leads, its picture, its caption's title and line, and the word in its window's
/// bar.
/// </summary>
/// <param name="Title">The caption's title, such as "A guesthouse".</param>
/// <param name="Line">The caption's one line under it.</param>
/// <param name="Image">The photograph's address.</param>
/// <param name="Href">Where pressing it goes.</param>
/// <param name="Address">The word in its window's bar, where a browser shows an address, such as "seemeeu.example".</param>
public sealed record Shot(string Title, string Line, string Image, string Href, string Address);
