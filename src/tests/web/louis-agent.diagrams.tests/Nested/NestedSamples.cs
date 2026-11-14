namespace LouisAgent.Diagrams.Tests.Nested;

/// <summary>
/// A nested picture the tests share (F134.7): a small start that holds a stage, which holds a deck and a dock,
/// with a page before it and a script after it. Small enough to reason about, with every part of the kind in it.
/// </summary>
internal static class NestedSamples
{
    /// <summary>
    /// The start: it holds which demo is forward, hands it down to three pins and hears it back from two. The deck
    /// holds a window, and beside the window a ghost.
    /// </summary>
    public const string Start = """
                                {
                                  "kind": "nested",
                                  "title": "The start: what it is made of",
                                  "caption": "One thing held, handed down and said back up.",
                                  "nested": {
                                    "subtitle": "louis-agent.web, Site / Showroom / Start",
                                    "root": {
                                      "id": "start", "name": "ShowroomStart", "kind": "component", "tone": "green",
                                      "aside": "Components/Site/Showroom/Start",
                                      "state": {
                                        "says": "which demo is forward",
                                        "down": [ "stage.Accent", "deck.Forward", "dock.Current" ],
                                        "up": [ "deck.ForwardChanged", "dock.Chosen" ]
                                      },
                                      "holds": [
                                        { "id": "stage", "name": "LitStage", "kind": "general part", "tone": "green",
                                          "in": [ "Accent" ],
                                          "holds": [
                                            { "id": "deck", "name": "Deck", "kind": "templated", "tone": "green",
                                              "note": "one forward, the rest behind it",
                                              "in": [ "Items", "Forward" ], "out": [ "ForwardChanged" ],
                                              "holds": [
                                                { "id": "window", "name": "FramedWindow", "tone": "green",
                                                  "in": [ "Address", "Lit" ] },
                                                { "id": "plain", "name": "PlainBody", "tone": "green",
                                                  "note": "when a demo names no window", "ghost": true }
                                              ] },
                                            { "id": "dock", "name": "Dock", "kind": "general part", "tone": "green",
                                              "in": [ "Items", "Current" ], "out": [ "Chosen" ] }
                                          ] }
                                      ]
                                    },
                                    "around": [
                                      { "id": "page", "name": "DemosPage", "side": "before", "kind": "page",
                                        "heading": "The site's page", "tone": "blue",
                                        "gives": [ "Featured", "Group" ] },
                                      { "id": "demos", "name": "ShowroomDemos", "side": "before", "tone": "teal",
                                        "heading": "What the start reads", "note": "the site's list", "reads": true },
                                      { "id": "script", "name": "Deck.razor.js", "side": "after", "to": "deck",
                                        "heading": "Runs in the browser", "code": "watch, bring",
                                        "note": "says which window a swipe left in the middle",
                                        "calls": "calls", "back": "Viewed" }
                                    ]
                                  }
                                }
                                """;

    /// <summary>The sample with one piece of its text swapped for another, to make it wrong in one place.</summary>
    public static string StartWith(string old, string instead)
    {
        string swapped = Start.Replace(old, instead, StringComparison.Ordinal);
        Assert.That(swapped, Is.Not.EqualTo(Start), $"the sample has no {old} to swap");
        return swapped;
    }
}
