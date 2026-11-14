namespace LouisAgent.Site.Components.Showroom.Windows;

/// <summary>
/// What each business's window on the start says of its recorded talk (F134.1): the first thing its visitor asked,
/// one whole sentence of what its agent answered, and the place the agent showed.
/// </summary>
/// <remarks>
/// The words are written here a second time: the question is on the demo's page and the answer is in its recording.
/// <c>WindowWordsTests</c> holds each to its source, so a new recording fails the window until its words follow.
/// </remarks>
public static class SiteWindowTalks
{
    /// <summary>The guesthouse's: a weekend with the dog, and the one room that takes dogs.</summary>
    public static WindowTalk Seemeeu { get; } = new(
        "Hi, do you have a sea-view room for this Friday and Saturday night? We are two, and we would like to " +
        "bring our dog.",
        "Dogs are only welcome in the garden room, a double with its own gate to the fenced garden, at R1,250 a " +
        "night plus R150 for your dog, so the sea-view doubles (R1,450 a night) can't take your dog.",
        "/rooms/garden",
        "recordings/showroom/seemeeu-guesthouse-1.sse");

    /// <summary>The restaurant's: Sunday lunch for five, two of them small.</summary>
    public static WindowTalk Wheatfield { get; } = new(
        "Hi, are you open for lunch on Sunday? We are a family of five, with two small kids.",
        "We're open for lunch on Sunday from 12:00 to 15:00, and the children's menu is R75 a dish.",
        "/menu/children",
        "recordings/showroom/wheatfield-kitchen-1.sse");

    /// <summary>The law practice's: a question it may not answer, and where it sends the visitor instead.</summary>
    public static WindowTalk Koringland { get; } = new(
        "Good morning. I need to change my will after my divorce. What should I put in it?",
        "What goes in your will is advice I can't give, but the page's \"After a divorce\" section explains how the " +
        "law treats a will made before a divorce, so I'll open it for you.",
        "/practice/wills/after-divorce",
        "recordings/showroom/koringland-attorneys-1.sse");
}
